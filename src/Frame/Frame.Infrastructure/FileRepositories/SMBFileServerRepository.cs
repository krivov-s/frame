using Frame.App;
using Frame.App.FileRepositories;
using Frame.Shared;
using Microsoft.Extensions.Logging;
using SMBLibrary;
using SMBLibrary.Client;

namespace Frame.Infrastructure.FileRepositories
{
    public class SMBFileServerRepository : IFilesRepository
    {
        private readonly ILogger<SMBFileServerRepository> _logger;
        private readonly SMBSettings _smbSettings;

        private static readonly SMB2Client _client = new();

        public SMBFileServerRepository(ILogger<SMBFileServerRepository> logger, IFileRepositirySettings settings) 
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            SMBSettings? smbSettings = settings as SMBSettings;

            if (smbSettings == null)
            {
                _logger?.LogError("{Repository}: переданный объект с настройками не является объектом с типом {tSettings}"
                    ,nameof(SMBFileServerRepository), nameof(SMBSettings));
                throw new ArgumentNullException(nameof(settings));
            }
            
            _smbSettings = smbSettings;
        }

        public Task<Result> DeleteFileAsync(string strKey)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    var connectionResult = Connect();
                    if (!connectionResult)
                    {
                        _logger.LogInformation("Ошибка при подключении к файловому хранилищу.");
                        return Task.FromResult(Result.Error("Ошибка при подключении к файловому хранилищу."));
                    }
                }

                var sharePath = GetSharePath();

                if (string.IsNullOrEmpty(sharePath))
                {
                    _logger.LogInformation("Ошибка в пути подключения к файловому хранилищу.");
                    return Task.FromResult(Result.Error("Ошибка в пути подключения к файловому хранилищу."));
                }

                var fileStore = _client.TreeConnect(sharePath, out NTStatus status);

                strKey = strKey.Replace("/", "\\");

                status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, strKey, AccessMask.GENERIC_WRITE | AccessMask.DELETE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

                if (status == NTStatus.STATUS_SUCCESS)
                {
                    FileDispositionInformation fileDispositionInformation = new FileDispositionInformation();
                    fileDispositionInformation.DeletePending = true;
                    status = fileStore.SetFileInformation(fileHandle, fileDispositionInformation);
                    var deleteSucceeded = (status == NTStatus.STATUS_SUCCESS);
                    status = fileStore.CloseFile(fileHandle);
                }

                status = fileStore.Disconnect();
                _logger.LogInformation("Файл успешно удалён из шары.");
                return Task.FromResult(Result.Success);
            }
            catch (Exception ex)
            {
                var error = $"Ошибка при получении файла {ex.Message}";
                _logger.LogInformation(ex, error);
                return Task.FromResult(Result.Error(error, ex));
            }
        }

        public async Task<Result<byte[]>> GetFileAsync(string strKey)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    var connectionResult = Connect();
                    if (!connectionResult)
                    {
                        _logger.LogInformation("Ошибка при подключении к файловому хранилищу.");
                        return Result<byte[]>.Error("Ошибка при подключении к файловому хранилищу.");
                    }
                }

                var sharePath = GetSharePath();
                _logger.LogInformation($"Путь подключения: {sharePath}");

                if (string.IsNullOrEmpty(sharePath))
                {
                    _client.Disconnect();
                    _logger.LogInformation("Ошибка в пути подключения к файловому хранилищу.");
                    return Result<byte[]>.Error("Ошибка в пути подключения к файловому хранилищу.");
                }

                var fileStore = _client.TreeConnect(sharePath, out NTStatus status);

                strKey = strKey.Replace("/", "\\");

                status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, strKey, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
                _logger.LogInformation($"Создан промежуточный файл {strKey}, статус: {status}  {fileHandle}, {fileStatus}");

                if (status == NTStatus.STATUS_SUCCESS)
                {
                    var stream = new MemoryStream();
                    byte[] data;
                    long bytesRead = 0;
                    while (true)
                    {
                        status = fileStore.ReadFile(out data, fileHandle, bytesRead, (int)_client.MaxReadSize);
                        if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                        {
                            _client.Disconnect();
                            return Result<byte[]>.Error("Ошибка при получении файла");
                        }

                        if (status == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
                        {
                            _client.Disconnect();
                            _logger.LogInformation($"Успешно получен файл {strKey} размером {stream.Length} байт.");
                            return Result<byte[]>.Success(stream.ToArray());
                        }
                        bytesRead += data.Length;
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }
                status = fileStore.CloseFile(fileHandle);
                status = fileStore.Disconnect();

                _client.Disconnect();
                _logger.LogError("Ошибка при получении файла");
                return Result<byte[]>.Error("Ошибка при получении файла");
            }
            catch (Exception ex) 
            {
                var error = $"Ошибка при получении файла {ex.Message}";
                _logger.LogInformation(ex, error);
                return Result<byte[]>.Error(error, ex);
            }
        }

        public async Task<Result> PutFileAsync(string strKey, Stream file)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    var connectionResult = Connect();
                    if (!connectionResult)
                    {
                        _client.Disconnect();
                        _logger.LogInformation("Ошибка при подключении к файловому хранилищу.");
                        return Result.Error("Ошибка при подключении к файловому хранилищу.");
                    }
                }

                var sharePath = GetSharePath();

                if (string.IsNullOrEmpty(sharePath))
                {
                    _client.Disconnect();
                    _logger.LogInformation("Ошибка в пути подключения к файловому хранилищу.");
                    return Result.Error("Ошибка в пути подключения к файловому хранилищу.");
                }

                var fileStore = _client.TreeConnect(sharePath, out NTStatus status);

                strKey = strKey.Replace("/", "\\");

                status = fileStore.CreateFile(out object fileHandle, out FileStatus fileStatus, strKey, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
                _logger.LogInformation($"Успешно создан промежуточный файл: {fileStatus}. Статус операции: {status}, ключ: {strKey}");

                if (status == NTStatus.STATUS_OBJECT_NAME_COLLISION)
                {
                    await DeleteFileAsync(strKey);
                    _logger.LogInformation($"Удалён файл {strKey} для перезаписи.");

                    status = fileStore.CreateFile(out fileHandle, out fileStatus, strKey, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
                    _logger.LogInformation($"Успешно создан промежуточный файл: {fileStatus}. Статус операции: {status}, ключ: {strKey}");
                }

                if (status != NTStatus.STATUS_SUCCESS)
                {
                    status = fileStore.Disconnect();
                    _client.Disconnect();
                    _logger.LogError($"Ошибка при перемещении файла в хранилище. Статус: {status}. Тип ошибки: {fileStatus}. Имя файла: {strKey}");
                    return Result.Error("Ошибка при перемещении файла в хранилище.");
                }

                _logger.LogInformation($"Operation status: {status}. File info: {file.Length} bytes, {file.Position}");

                if (status == NTStatus.STATUS_SUCCESS)
                {
                    int writeOffset = 0;
                    while (file.Position < file.Length)
                    {
                        byte[] buffer = new byte[(int)_client.MaxWriteSize];
                        int bytesRead = await file.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead < (int)_client.MaxWriteSize)
                        {
                            Array.Resize<byte>(ref buffer, bytesRead);
                        }
                        int numberOfBytesWritten;
                        status = fileStore.WriteFile(out numberOfBytesWritten, fileHandle, writeOffset, buffer);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            _client.Disconnect();
                            _logger.LogError("Ошибка при записи файла.");
                            return Result.Error("Ошибка при записи файла.");
                        }
                        writeOffset += bytesRead;
                    }
                    _logger.LogInformation($"Записано {writeOffset} байт");
                    status = fileStore.CloseFile(fileHandle);

                    _client.Disconnect();
                    _logger.LogInformation($"Файл успешно перемещён в хранилище. Статус: {status}");
                    return Result.Success;
                }

                status = fileStore.Disconnect();
                _client.Disconnect();
                _logger.LogError($"Ошибка при перемещении файла в хранилище. Статус: {status}");
                return Result.Error("Ошибка при перемещении файла в хранилище.");
            }
            catch (Exception ex) 
            {
                var error = $"Ошибка при получении файла {ex.Message}";
                _logger.LogInformation(ex, error);
                return Result<byte[]>.Error(error, ex);
            }
        }

        public Task<Result<List<string>>> LoadFileListAsync(string path)
        {
            throw new NotImplementedException();
            // try
            // {
            //     if (!_client.IsConnected)
            //     {
            //         var connectionResult = Connect();
            //         if (!connectionResult)
            //         {
            //             _client.Disconnect();
            //             _logger.LogInformation("Ошибка при подключении к файловому хранилищу.");
            //             return Result<List<string>>.Error("Ошибка при подключении к файловому хранилищу.");
            //         }
            //     }
            //
            //     var sharePath = GetSharePath();
            //
            //     if (string.IsNullOrEmpty(sharePath))
            //     {
            //         _client.Disconnect();
            //         _logger.LogInformation("Ошибка в пути подключения к файловому хранилищу.");
            //         return Result<List<string>>.Error("Ошибка в пути подключения к файловому хранилищу.");
            //     }
            //
            //     var fileStore = _client.TreeConnect(sharePath, out NTStatus status);
            //     if(fileStore == null) 
            //     {
            //         _client.Disconnect();
            //         _logger.LogInformation("Ошибка при подключении к файловому хранилищу.");
            //         return Result<List<string>>.Error("Ошибка при подключении к файловому хранилищу.");
            //     }
            //     _logger.LogInformation($"Подключено к папке {sharePath}");
            //
            //     path = path.Replace("/", "\\");
            //
            //     var fileNames = new List<string>();
            //     GetSubfolderFileNames(fileNames, fileStore, path);
            //
            //     return fileNames;
            // }
            // catch (Exception ex)
            // {
            //     var error = $"Ошибка при получении файла {ex.Message}";
            //     _logger.LogInformation(ex, error);
            //     return Result<List<string>>.Error(error, ex);
            // }
        }

        /// <summary>
        /// Метод управляет подключением к файловому хранилищу
        /// данные для подключения должны быть прописаны в
        /// vault и возвращаться через класс SMBSettings
        /// </summary>
        private bool Connect()
        {
            if (_client.IsConnected)
                return true;

            var fullServerPath = _smbSettings.Path;
            fullServerPath = fullServerPath.Replace(@"\\", "");
            var serverPathArray = fullServerPath.Split('\\');

            if (fullServerPath.Length == 0)
            {
                _client.Disconnect();
                _logger.LogError("Ошибка в прилагаемом пути к файловому хранилищу");
                return false;
            }

            var server = serverPathArray[0];

            var isConnected = _client.Connect(server, SMBTransportType.DirectTCPTransport);
            if (!isConnected) 
            {
                _logger.LogError($"Ошибка при подключении к файловому хранилищу по адресу {server}.");
                return false;
            }
            
            var password = _smbSettings.Password;

            if (string.IsNullOrEmpty(password))
            {
                _logger.LogError("В конфигурации отсутствует параметр Password.");
                return false;
            }

            var domainLogin = _smbSettings.Login;

            if (string.IsNullOrEmpty(domainLogin))
            {
                _logger.LogError("В конфигурации отсутствует параметр Login.");
                return false;
            }

            domainLogin = domainLogin.Replace(@"\\", "");
            var domainLoginArray = domainLogin.Split('\\');

            //если не два - exception
            if (domainLoginArray.Length != 2)
            {
                _logger.LogError($"Ошибка в прилагаемом пути к файловому хранилищу {domainLogin}");
                return false;
            }

            var domain = domainLoginArray[0];

            var login = domainLoginArray[1];

            _logger.LogInformation($"Попытка подключения к домену {domain}");
            var status = _client.Login(domain, login, password);

            if(status == NTStatus.STATUS_SUCCESS)
            {
                _logger.LogInformation("Успешное подключение к файловому хранилищу.");
                return true;
            }

            _logger.LogError($"Ошибка при подключении к файловому хранилищу. Статус ошибки подключения: {status}");
            return false;
        }

        private string GetSharePath()
        {
            var sharePath = _smbSettings.Path;

            if (string.IsNullOrEmpty(sharePath))
            {
                _client.Disconnect();
                _logger.LogError("В конфигурации отсутствует параметр Path");
                return string.Empty;
            }

            sharePath = sharePath.Replace(@"\\", "");
            var sharePathArray = sharePath.Split('\\');

            //переделать
            if (sharePathArray.Length == 0)
            {
                _client.Disconnect();
                _logger.LogError("Ошибка в прилагаемом пути к файловому хранилищу");
                return string.Empty;
            }

            return sharePathArray[1];
        }

        private static void GetSubfolderFileNames(List<string> fileNames, ISMBFileStore fileStore, string directoryName)
        {
            var status = fileStore.CreateFile(out object directoryHandle, out FileStatus fileStatus, directoryName, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
            status = fileStore.QueryDirectory(out List<QueryDirectoryFileInformation> fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);

            var fileInfo = fileList.Cast<FileDirectoryInformation>();
            fileInfo = fileInfo.Where(x => !x.FileName.StartsWith('.') && !x.FileName.EndsWith('.'));

            var directoryInfo = fileInfo.Where(x => x.FileAttributes == SMBLibrary.FileAttributes.Directory);
            fileInfo = fileInfo.Where(x => x.FileAttributes != SMBLibrary.FileAttributes.Directory);

            foreach(var info in fileInfo)
            {
                fileNames.Add($"{directoryName}\\{info.FileName}");
            }

            foreach(var directory in directoryInfo)
            {
                var directoryPath = string.IsNullOrEmpty(directoryName) ? directory.FileName : $"{directoryName}\\{directory.FileName}";
                GetSubfolderFileNames(fileNames, fileStore, directoryPath);
            }
        }
    }
}
