using System.Dynamic;
using Frame.App.FileRepositories;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.FileDocuments;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.FileRepositories;

public class FileDocumentService(
    IFilesRepository fileRepository,
    IFileRepositirySettings fileRepositorySettings,
    ILogger<FileDocumentService> logger): IFileDocumentService
{
    private const string _errFmt = "{Err}";
    
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    /// <param name="fileDocument">Подготовленный объект</param>
    /// <param name="fileStream">Поток с файлом, который нужно сохранить</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="storage">Хранилище, в которое нужно сохранить fileDocument</param>
    /// <returns></returns>
    public async Task<Result> SaveFileToDocumentAsync(FileDocument fileDocument, Stream fileStream,
        string fileName, IObjectStorage? storage = null)
    {
        // Определяем предельный размер файла для загрузки
        int maxFileSize = fileRepositorySettings?.GetMaxFileSize() ?? 2048000;
        if (fileStream.Length > maxFileSize)
        {
            string err =
                $"Размер загружаемого файла [{fileStream.Length}] больше предельно допустимого [{maxFileSize}]";
            logger.LogError(_errFmt, err);
            return Result.Error(err);
        }

        // Если у текущего объекта FileDocument установлен FileKey - он берется как адрес (имя) файла для сохранения в хранилище
        // Если не установлен - берем BaseEntity.Key от текущего fileDocument - это будет имя по-умолчанию.
        if (fileDocument.FileKey is null or { Length: <= 0 })
        {
            if (fileDocument.Key.Length == 0)
            {
                // Если у текущего fileDocument Key еще не установлен - значит сначала нужно его сохранить!
                // Внимание!!! Возможен нежелательный побочный эффект: если в objectStorage уже добавлены какие-либо
                // изменения по другим объектам, они также будут сохранены в БД!!!
                if (storage == null)
                {
                    string err = $"Для загрузки файла необходимо сначала сохранить в БД FileDocument," +
                                 $" необходимый для этого objectStorage в сервис не передан!";
                    logger.LogError(_errFmt, err);
                    return Result.Error(err);
                }

                storage.Add(fileDocument);
                Result resSave = await storage.SaveChangesAsync();
                if (resSave.IsError)
                {
                    return resSave;
                }
                if (fileDocument.Key.Length == 0)
                {
                    string err = $"У объекта {fileDocument} на установлено значение Key после сохранения!";
                    logger.LogError(_errFmt, err);
                    return Result.Error(err);
                }
            }

            
            string homePath = fileRepositorySettings?.GetDocumentsHomePath() ?? "";

            // Проверяем, если установлен FileDocumentType и у него установлен PathToFolder - до добавляем его к пути
            if (fileDocument.FileDocumentType != null && fileDocument.FileDocumentType.PathToFolder.Length > 0)
            {
                homePath = Path.Combine(homePath, fileDocument.FileDocumentType.PathToFolder);
            }

            fileDocument.FileKey = (homePath.Length > 0) ? Path.Combine(homePath, fileDocument.Key) : fileDocument.Key;
        }

        // Перезаписываем имя файла на то, которое загрузилось (если не задано вручную)
        if (!string.IsNullOrEmpty(fileName))
        {
            fileDocument.FileName = fileName;
        }

        Result res = await fileRepository.PutFileAsync(fileDocument.FileKey, fileStream);

        if (res.IsError)
        {
            return Result.Error($"Ошибка загрузки файла в хранилище: {res.ErrorResult}");
        }

        if (storage != null)
        {
            storage.Add(fileDocument);
            Result resSave = await storage.SaveChangesAsync();
            if (resSave.IsError)
            {
                string err =
                    $"Файл в хранилище загружен, но произошла ошибка при сохранении записи в БД: {resSave.ErrorResult}";
                logger.LogError(_errFmt, err);
                return Result.Error(err);
            }
        }
        return Result.SuccessWithMessage("Файл успешно загружен в хранилище.");
    }
    

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    /// <param name="fileDocuments"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public async Task<Result<dynamic>> LoadTaggedContentAsync(List<FileDocument> fileDocuments, Func<FileDocument, bool>? filter = null)
    {
        dynamic expando = new ExpandoObject();
        
        IDictionary<string, object?> docList = expando;
        
        foreach (FileDocument fileDocument in fileDocuments)
        {
            // Если тег отсутствует - пропускаем документ
            if (string.IsNullOrEmpty(fileDocument.UserTag)) continue;
                
            // Если ключ файла отсутствует - пропускаем документ
            if (string.IsNullOrEmpty(fileDocument.FileKey)) continue;
            
            // Если фильтр установлен и результат сравнения = false - пропускаем документ
            if(filter != null && !filter(fileDocument)) continue;

            List<string> tags = fileDocument.UserTag.Split('#',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            if(tags.Count == 0) continue;
            
            Result<byte[]> resFileData = await fileRepository.GetFileAsync(fileDocument.FileKey);
            if (resFileData.IsErrorOrNull)
            {
                string err = $"Ошибка загрузки файла для документа {fileDocument.FileName} с тегом {fileDocument.UserTag}: " +
                             $"{resFileData.ErrorResult}";
                logger.LogError(_errFmt, err);                
                return Result<dynamic>.Error(err);
            }

            foreach (var tag in tags)
            {
                docList.Add(tag, resFileData.Value!);
            }
        }
        
        return Result<dynamic>.Success(expando);
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    /// <param name="fileDocument"><inheritdoc /></param>
    /// <returns></returns>
    public async Task<Result<byte[]>> LoadDocumentContentAsync(FileDocument fileDocument)
    {
        if (string.IsNullOrEmpty(fileDocument.FileKey))
        {
            string err = $"У объекта {nameof(FileDocument)} {fileDocument} не установлен ключ файла";
            logger.LogError(_errFmt, err);                
            return Result<byte[]>.Error(err);
        }
        
        Result<byte[]> resFileData = await fileRepository.GetFileAsync(fileDocument.FileKey);
        if (resFileData.IsErrorOrNull)
        {
            string err = $"Ошибка загрузки файла для {nameof(FileDocument)} {fileDocument}: " +
                         $"{resFileData.ErrorResult}";
            logger.LogError(_errFmt, err);                
            return Result<byte[]>.Error(err);
        }
        
        return resFileData;
    }

    
}
