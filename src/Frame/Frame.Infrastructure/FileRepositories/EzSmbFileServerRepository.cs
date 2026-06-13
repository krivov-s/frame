using EzSmb;
using EzSmb.Params;
using Frame.App.FileRepositories;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.FileRepositories;

public class EzSmbFileServerRepository : IFilesRepository
{
    private readonly ILogger<EzSmbFileServerRepository> _logger;
    private readonly SMBSettings _smbSettings;

    private Node? _rootNode;

    public EzSmbFileServerRepository(IFileRepositirySettings settings, ILogger<EzSmbFileServerRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        SMBSettings? smbSettings = settings as SMBSettings;

        if (smbSettings == null)
        {
            _logger.LogError("{Repository}: переданный объект с настройками не является объектом с типом {Settings}"
                , nameof(SMBFileServerRepository), nameof(SMBSettings));
            throw new ArgumentNullException(nameof(settings));
        }

        _smbSettings = smbSettings;
        // Убираем двойной обратный слэш (как правило может быть расположен в начале адреса)
        _smbSettings.Path = _smbSettings.Path.Replace(@"\\", "");
        // Меняем обратный слэш на прямой 
        _smbSettings.Path = _smbSettings.Path.Replace(@"\", "/");
    }

    public async Task<Result> PutFileAsync(string strKey, Stream file)
    {
        try
        {
            if (string.IsNullOrEmpty(strKey))
            {
                string err = "Не указан путь к файлу";
                _logger.LogError(err);
                return Result.Error(err);
            }

            if (file == null)
            {
                string err = "Не передан файл для записи";
                _logger.LogError(err);
                return Result.Error(err);
            }

            Node rootNode = await ConnectAndGetRootNodeAsync();

            // Меняем обратный слэш на прямой, чтобы функции Path сработали и на Linux и на windows
            // Хотя библиотека вроде бы нормально понимает и прямые и обратные слэши
            string tmp = strKey.Replace(@"\", "/");
            string? directoryPath = Path.GetDirectoryName(tmp);
            
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Result r = await CheckAndCreateDirectoryAsync(rootNode, directoryPath, strKey);
                if (r.IsError)
                {
                    return r;
                }
            }

            bool bRes = await rootNode.Write(file, strKey);
            if (!bRes)
            {
                string errList = GetErrorString(rootNode);
                string err = $"Ошибка сохранения файла {strKey}: {errList}";
                _logger.LogError(err);
                return Result.Error($"Ошибка сохранения файла {strKey}");
            }
            return Result.Success;
        }
        catch (Exception ex)
        {
            string err = $"Ошибка сохранения файла {strKey}";
            _logger.LogError(ex, err);
            return Result.Error(err, ex);
        }
    }

    private async Task<Result> CheckAndCreateDirectoryAsync(Node rootNode, string directoryPath, string strKey)
    {
        Node directoryNode = await rootNode.GetNode(directoryPath);
        if (directoryNode == null || directoryNode.HasError)
        {
            string errList = GetErrorString(rootNode);
            if (errList.Contains("не найден"))
            {
                // Пытаемся создать папку
                directoryNode = await rootNode.CreateFolder(directoryPath);
                if (directoryNode == null || directoryNode.HasError)
                {
                    string err = $"Ошибка сохранения файла {strKey}: не удалось создать папку {directoryPath}: {errList}";
                    _logger.LogError(err);
                    return Result.Error($"Ошибка сохранения файла {strKey}");
                }
            }
            else
            {
                string err = $"Ошибка сохранения файла {strKey}: {errList}";
                _logger.LogError(err);
                return Result.Error($"Ошибка сохранения файла {strKey}");
            }
        }
        return Result.Success;
    }
    
    public async Task<Result<byte[]>> GetFileAsync(string strKey)
    {
        try
        {
            if (string.IsNullOrEmpty(strKey))
            {
                const string err = "Не указан путь к файлу";
                _logger.LogError(err);
                return Result<byte[]>.Error(err);
            }

            Node rootNode = await ConnectAndGetRootNodeAsync();
            Node fileNode = await rootNode.GetNode(strKey);
            if (fileNode == null)
            {
                string errList = GetErrorString(rootNode);
                string err = $"Ошибка чтения файла {strKey}: {errList}";
                _logger.LogError(err);
                return Result<byte[]>.Error(err);
            }

            using MemoryStream readStream = await fileNode.Read();
            byte[] buffer = readStream.ToArray();
            return Result<byte[]>.Success(buffer);
        }
        catch (Exception ex)
        {
            string err = $"Ошибка чтения файла {strKey}";
            _logger.LogError(ex, err);
            return Result<byte[]>.Error(err, ex);
        }
    }

    public async Task<Result> DeleteFileAsync(string strKey)
    {
        try
        {
            if (strKey == "")
            {
                string err = "Не указан путь к файлу";
                _logger.LogError(err);
                return Result<byte[]>.Error(err);
            }

            Node rootNode = await ConnectAndGetRootNodeAsync();
            await rootNode.Delete(strKey);
            return Result.Success;
        }
        catch (Exception ex)
        {
            string err = $"Ошибка удаления файла {strKey}";
            _logger.LogError(ex, err);
            return Result.Error(err, ex);
        }
    }

    public async Task<Result<List<string>>> LoadFileListAsync(string path)
    {
        try
        {
            Node rootNode = await ConnectAndGetRootNodeAsync();

            var folderNode = await rootNode.GetNode(path);

            if (folderNode == null)
            {
                string err = $"Ошибка при открытии {path}";
                _logger.LogError(err);
                return Result<List<string>>.Error(err);
            }

            if (folderNode.Type != NodeType.Folder)
            {
                string err = $"Путь {path} не является папкой";
                _logger.LogError(err);
                return Result<List<string>>.Error(err);
            }

            Node[] nodes = await folderNode.GetList();
            List<string> listFiles = await GetFilesFromNodesRecursiveAsync(nodes);
            return Result<List<string>>.Success(listFiles);
        }
        catch (Exception ex)
        {
            // Логирование ошибки
            string err = $"Ошибка получения списка файлов из папки {path}";
            _logger.LogError(ex, err);
            return Result<List<string>>.Error(err, ex);
        }
    }

    private async Task<List<string>> GetFilesFromNodesRecursiveAsync(Node[] nodes)
    {
        if (nodes.Length == 0)
        {
            return [];
        }

        // Загоняем массив в список для удобства
        List<Node> listNodes = new List<Node>();
        listNodes.AddRange(nodes);

        // Готовим место для результирующего списка
        List<string> listFiles = [];

        // Все файлы сразу копируем в результирующий список
        foreach (Node fileNode in listNodes.Where(n => n.Type == NodeType.File).ToList())
        {
            // Берем только путь к файлу, без ShareName и Ip
            var relativePath = fileNode.PathSet.ElementsPath;
            listFiles.Add(relativePath);
        }

        // Получаем список папок
        List<Node> listNodeFolders = listNodes.Where(n => n.Type == NodeType.Folder).ToList();

        // В цикле зачитываем содержимое каждой папки и рекурсивно обрабатываем
        foreach (Node folderNode in listNodeFolders)
        {
            Node[] folderNodes = await folderNode.GetList();
            List<string> listFolderFiles = await GetFilesFromNodesRecursiveAsync(folderNodes);
            listFiles.AddRange(listFolderFiles);
        }

        return listFiles;
    }

    private async Task<Node> ConnectAndGetRootNodeAsync()
    {
        if (_rootNode != null)
        {
            return _rootNode;
        }

        if (_smbSettings.Path == "")
        {
            throw new ArgumentException("В конфигурации отсутствует параметр Path");
        }

        if (string.IsNullOrEmpty(_smbSettings.Login))
        {
            string err = $"В конфигурации отсутствует параметр Login.";
            throw new ArgumentException(err);
        }

        // Path и Login могут прийти в формате:
        //   domain/login
        //   //domain/login
        //   domain\login
        //   \\domain\login
        //   login
        // Или иные комбинации прямых/обратных слешей

        // 1. Разбираемся с путем
        string sharePath = NormalizePath(_smbSettings.Path);

        // 2. Разбираемся с логином. Нам нужен только логин, домен игнорируем.
        string domainLogin = _smbSettings.Login;

        // Меняем все прямые слэши на обратные
        domainLogin = domainLogin.Replace("/", @"\");
        // Убираем два первых слэша чтобы не мешались
        domainLogin = domainLogin.Replace(@"\\", "");
        // Разбиваем на domain\login
        string[] domainLoginArray = domainLogin.Split('\\');
        // Нам из этой конструкции нужен только логин
        string login = (domainLoginArray.Length == 2) ? domainLoginArray[1] : domainLogin;

        // 3. Выполняем connect
        var paramSet = new ParamSet
        {
            UserName = login,
            Password = _smbSettings.Password
        };

        _rootNode = await Node.GetNode(sharePath, paramSet, true);
        if (_rootNode == null)
        {
            string err = $"Неизвестная ошибка при установлении соединения с smb сервером";
            throw new Exception(err);
        }

        return _rootNode;
    }

    private static string GetErrorString(Node rootNode)
    {
        string errList = "";
        if (rootNode.HasError)
        {
            errList = rootNode.Errors[^1];
            if (errList.ToLower().Contains("path not found"))
            {
                errList = $"Заданный путь не найден";
            }
        }

        return errList;
    }

    /// <summary>
    /// Замена прямых слешей на обратные и добавление двух обратных слешей в начало пути
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static string NormalizePath(string path)
    {
        // Меняем все прямые слэши на обратные
        path = path.Replace("/", @"\");
        // Если отсутствует - добавляем в начало два обратных слэша
        if (!path.StartsWith(@"\\"))
        {
            path = @"\\" + path;
        }

        return path;
    }
}