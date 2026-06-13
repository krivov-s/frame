using System.Collections.Concurrent;
using Frame.App.FileRepositories;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.FileRepositories;

/// <summary>
/// Хранилище файлов на обычном файл-сервере.
/// Конфигурация (путь) считывается из appsettings.json
/// </summary>
public class FileServerRepository: IFilesRepository
{
    private readonly ILogger<FileServerRepository>? _logger;
    private readonly string _basePath;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

    public FileServerRepository(IFileRepositirySettings settings, ILogger<FileServerRepository> logger)
    {
        _logger = logger;
        FileServerSettings? fileServerSettings = settings as FileServerSettings;

        if (fileServerSettings == null)
        {
            _logger?.LogError("{Repository}: переданный объект с настройками не является объектом с типом {tSettings}"
                ,nameof(FileServerRepository), nameof(FileServerSettings));
            throw new ArgumentNullException(nameof(settings));
        }
        
        if (string.IsNullOrEmpty(fileServerSettings.FileServerPath))
        {
            _logger?.LogError("{FileServerRepository}: в настройках не указан путь к папке репозитория"
                ,nameof(FileServerRepository));
            throw new ArgumentNullException(nameof(fileServerSettings.FileServerPath));
        }

        _basePath = fileServerSettings.FileServerPath;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        // Создаем директорию, если она не существует
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }
    
    /// <summary>
    /// Сохранение файла
    /// </summary>
    /// <param name="strKey">Имя файла, при необходимости - с указанием пути относительно корневой папки на сервере </param>
    /// <param name="file">Сохраняемый поток</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<Result> PutFileAsync(string strKey, Stream file)
    {
        if (string.IsNullOrEmpty(strKey))
        {
            _logger?.LogError("{FileServerRepository}: аргумент {StrKey} не задан"
                ,nameof(FileServerRepository.PutFileAsync)
                ,nameof(strKey));
            return Result.Error("Не указан путь к файлу");
        }

        if (file == null)
        {
            _logger?.LogError("{FileServerRepository}: аргумент {File} не задан"
                ,nameof(FileServerRepository.PutFileAsync)
                ,nameof(file));
            return Result.Error("Не передан файл для записи");
        }

        var fileLock = GetLock(strKey);
        try
        {
            await fileLock.WaitAsync();
            
            string filePath = Path.Combine(_basePath, strKey);
            string? directoryPath = Path.GetDirectoryName(filePath);

            if (directoryPath == null)
            {
                _logger?.LogError("{FileServerRepository}: не удалось сформировать путь для записи файла"
                    ,nameof(FileServerRepository.PutFileAsync));
                return Result.Error("Не удалось сформировать путь для записи файла");
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Создаем временный файл и только потом перемещаем его
            string tempFilePath = Path.Combine(directoryPath, $"{Guid.NewGuid()}.tmp");

            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await file.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }

                // Атомарная операция перемещения файла
                File.Move(tempFilePath, filePath, true);

                return Result.Success;            
            }
            catch (Exception)
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger?.LogError(deleteEx, "Ошибка удаления временного файла: {TempPath}", tempFilePath);
                    }
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка сохранения файла {Key}", strKey);
            return Result.Error($"Ошибка при сохранении файла {strKey}: {ex.Message}", ex);
        }
        finally
        {
            fileLock.Release();
            CleanupLock(strKey);
        }
    }
    
    /// <summary>
    /// Чтение файла
    /// </summary>
    /// <param name="strKey">Имя файла, при необходимости - с указанием пути относительно корневой папки на сервере </param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<Result<byte[]>> GetFileAsync(string strKey)
    {
        if (string.IsNullOrEmpty(strKey))
        {
            _logger?.LogError("{FileServerRepository}: аргумент {strKey} не задан"
                ,nameof(FileServerRepository.GetFileAsync)
                ,nameof(strKey));
            return Result<byte[]>.Error("Не указан путь к файлу");
        }

        var fileLock = GetLock(strKey);
        try
        {
            await fileLock.WaitAsync();

            string filePath = Path.Combine(_basePath, strKey);

            if (!File.Exists(filePath))
            {
                string err = $"Файл не найден: {strKey}";
                _logger?.LogWarning("{Err}", err);
                return Result<byte[]>.Error(err);
            }

            // Открываем файл с разделяемым доступом на чтение
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length);
                return Result<byte[]>.Success(buffer);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка чтения файла {Key}", strKey);
            return Result<byte[]>.Error($"Ошибка при чтении файла: {ex.Message}", ex);
        }
        finally
        {
            fileLock.Release();
            CleanupLock(strKey);
        }
    }

    public async Task<Result> DeleteFileAsync(string strKey)
    {
        if (string.IsNullOrEmpty(strKey))
        {
            _logger?.LogError("{FileServerRepository}: аргумент {strKey} не задан"
                ,nameof(FileServerRepository.DeleteFileAsync)
                ,nameof(strKey));
            return Result<byte[]>.Error("Не указан путь к файлу");
        }

        var fileLock = GetLock(strKey);
        try
        {
            await fileLock.WaitAsync();

            string filePath = Path.Combine(_basePath, strKey);

            if (!File.Exists(filePath))
            {
                _logger?.LogWarning("Попытка удаления несуществующего файла: {Key}", strKey);
                return Result.Error("Файл не найден");
            }

            try
            {
                File.Delete(filePath);

                // Проверяем, пуста ли директория после удаления файла
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null)
                {
                    if (Directory.Exists(directoryPath) && !Directory.EnumerateFileSystemEntries(directoryPath).Any())
                    {
                        Directory.Delete(directoryPath);
                    }
                }

                return Result.Success;
            }
            catch (IOException ex)
            {
                _logger?.LogError(ex, "Ошибка ввода-вывода при удалении файла: {Key}", strKey);
                return Result.Error($"Ошибка при удалении файла: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger?.LogError(ex, "Отсутствуют права на удаление файла: {Key}", strKey);
                return Result.Error($"Недостаточно прав: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка при удалении файла: {Key}", strKey);
                return Result.Error($"Ошибка при удалении файла: {ex.Message}");
            }
        }
        finally
        {
            fileLock.Release();
            CleanupLock(strKey);
        }
    }
    
    private SemaphoreSlim GetLock(string key)
    {
        return _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }
    
    private void CleanupLock(string key)
    {
        // Периодически очищаем неиспользуемые блокировки
        if (_locks.TryGetValue(key, out var lockItem) && lockItem.CurrentCount == 1)
        {
            _locks.TryRemove(key, out _);
            lockItem.Dispose();
        }
    }

    public async Task<Result<List<string>>> LoadFileListAsync(string path)
    {
        List<string> result = [];

        try
        {
            // Проверяем существование директории
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Директория не найдена: {path}");
            }

            // Получаем все файлы в текущей директории
            foreach (string file in Directory.GetFiles(path))
            {
                result.Add(file);
            }

            // Рекурсивно обходим все поддиректории
            foreach (string dir in Directory.GetDirectories(path))
            {
                Result<List<string>> resSubList = await LoadFileListAsync(dir);
                if (resSubList is { IsError: false, Value: not null })
                {
                    result.AddRange(resSubList.Value);    
                }
            }
        }
        catch (Exception ex)
        {
            string err = "Ошибка при сканировании директории";
            _logger?.LogError(ex, err);
            return Result<List<string>>.Error(err);
        }

        return Result<List<string>>.Success(result);
    }
}