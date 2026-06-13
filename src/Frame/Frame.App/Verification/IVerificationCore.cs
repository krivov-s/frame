using Frame.Domain.Entities.Core.FileDocuments;
using Frame.Shared;

namespace Frame.App.Verification;

/// <summary>
/// Управление метаданными для сервиса верификации объектов системы
/// </summary>
public interface IVerificationCore
{
    /// <summary>
    /// Регистрация типа (модели) как верифицируемого. Может вызываться несоклько раз с разными путями к корневым объектам,
    /// т.е. один и тот же тип может оповещать о своих изменениях разные корневые объекты.
    /// </summary>
    /// <param name="entityType">Тип модели</param>
    /// <param name="isRoot">Данная модель является корневой - для нее должны создаваться VerificationRecord</param>
    /// <param name="pathToRoot">Путь к корневой модели, у которой должны отображаться изменения в случае изменения объекта заданного типа</param>
    public void RegisterEntityType(string entityType, bool isRoot, string pathToRoot);
    
    /// <summary>
    /// Регистрация владельца объекта <see cref="FileDocument"/>, которому необходимо сообщать об изменении <see cref="FileDocument"/> 
    /// </summary>
    /// <param name="entityType">Тип объекта, который нужно оповещать об изменениях</param>
    public void RegisterFileDocumentRoot(string entityType);

    /// <summary>
    /// Получение метаданных для заданного типа.
    /// </summary>
    /// <param name="entityType">Тип объекта, для которого требуются метаданные</param>
    /// <returns></returns>
    public Result<VerificationTypeMetadata> GetMetadata(string entityType);
}