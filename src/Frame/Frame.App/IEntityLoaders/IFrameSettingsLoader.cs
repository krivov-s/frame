using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core;
using Frame.Shared;

namespace Frame.App.IEntityLoaders;

public interface IFrameSettingsLoader
{
    /// <summary>
    /// Поиск, при отсутствии - создание и сохранение системных настроек, системного пользователя и его профиля
    /// </summary>
    /// <returns></returns>
    public Task<Result<FrameSettings>> InitFrameSettingsAsync();
    
    /// <summary>
    /// Чтение из заданного objectStorage системных настроек с целью дальнейшего редактирования и сохранения.
    /// </summary>
    /// <param name="objectStorage"></param>
    /// <returns></returns>
    public Task<Result<FrameSettings>> LoadFrameSettingsAsync(IObjectStorage objectStorage);
}