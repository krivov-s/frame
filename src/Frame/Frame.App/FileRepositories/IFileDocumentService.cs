using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.FileDocuments;
using Frame.Shared;

namespace Frame.App.FileRepositories;

public interface IFileDocumentService
{
    /// <summary>
    /// Сохранение файла в заданный <paramref name="fileDocument"/>.
    /// Все параметры (ключ, имя файла и пр.) должны быть
    /// предварительно установлены в атрибутах <paramref name="fileDocument"/>.
    /// </summary>
    /// <param name="fileDocument">Объект, описывающий сохраняемый файл</param>
    /// <param name="fileStream">Сохраняемый поток. Текущий указатель должен быть в начале потока (если применимо).</param>
    /// <param name="fileName">Имя сохраняемого файла. Если указано - существующее имя будет изменено.</param>
    /// <param name="storage">Хранилище, из которого прочитан <paramref name="fileDocument"/>.
    /// Если <paramref name="storage"/> указан то в него будет добавлен <paramref name="fileDocument"/>
    /// и будет вызван метод <see cref="IObjectStorage.SaveChangesAsync"/>.</param>
    /// <returns></returns>
    public Task<Result> SaveFileToDocumentAsync(FileDocument fileDocument, Stream fileStream, string fileName, IObjectStorage? storage = null);
    
    /// <summary>
    /// Чтение содержимого набора файл-документов в объект <see cref="ExpandoObject"/>.
    /// В <see cref="ExpandoObject"/> добавляются свойства с типом byte[] с именем равным значению свойства
    /// <see cref="FileDocument.UserTag"/> без стартового символа #. Если тег не проставлен - содержимое не считывается. 
    /// </summary>
    /// <param name="fileDocuments">Список файл=документов</param>
    /// <param name="filter">Условие отбора документов для чтения содержимого. Если не установлено - читаются все.</param>
    /// <returns><see cref="ExpandoObject"/> с набором свойств в соответствии с тегами документов.</returns>
    public Task<Result<dynamic>> LoadTaggedContentAsync(List<FileDocument> fileDocuments,
        Func<FileDocument, bool>? filter = null);
    
    /// <summary>
    /// Чтение содержимого файла в массив байт. Данный метод - простой прокси для <see cref="IFilesRepository.GetFileAsync"/>
    /// </summary>
    /// <param name="fileDocument">Файл-документ, содержимое которого нужно прочитать</param>
    /// <returns></returns>
    public Task<Result<byte[]>> LoadDocumentContentAsync(FileDocument fileDocument);
    
    
}