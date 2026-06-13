using Frame.App.EventBus.Events.Notify;
using Frame.App.IEntityRepositories;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Params;
using Frame.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Handlers.Notify
{
    public class UserAfterSaveNotificationHandler(
        ILogger<UserAfterSaveNotificationHandler> logger, 
        IUserSecurityDataManager securityDataManager,
        IUserProfileRepository repositoryPl) : INotificationHandler<EntityAfterSaveNotification<User>>
    {
        private readonly ILogger<UserAfterSaveNotificationHandler> _logger = 
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IUserSecurityDataManager _securityDataManager = 
            securityDataManager ?? throw new ArgumentNullException(nameof(securityDataManager));
        private readonly IUserProfileRepository _repositoryPl = 
            repositoryPl ?? throw new ArgumentNullException(nameof(repositoryPl));
        public async Task Handle(EntityAfterSaveNotification<User> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity == null)
            {
                _logger.LogError("В {Handler} передан нулевой объект!", nameof(UserAfterSaveNotificationHandler));
                return;
            }

            User user = notification.Entity;
            
            // Читаем список параметров из БД
            Result<UserProfile> resParamList = await _repositoryPl.LoadUserProfileAsync(user.Login);
            UserProfile entityParamList = (resParamList.IsError || resParamList.Value == null)
                ? new() { UserId = user.Id }
                : resParamList.Value;
            
            ParamList userParamList = entityParamList.GetParamList();
            
            // Проверяем наличие параметра в списке. Если нет - добавляем.
            Result<object?> resDarkTheme = userParamList.GetByName(ParamList.DefaultParamNames.UiDarkTheme);
            if (resDarkTheme.IsError)
            {
                await userParamList.AddParamAsync(ParamList.DefaultParamNames.UiDarkTheme, false, true, "Темный режим");
                entityParamList.SetParamList(userParamList);

                Result res = await _repositoryPl.SaveUserProfileAsync(entityParamList);

                if (!res.IsError)
                {
                    _logger.LogInformation("Для пользователя {Name} сохранен список параметров: установлен {Param}",
                        user.Login, ParamList.DefaultParamNames.UiDarkTheme);
                }
                else
                {
                    _logger.LogError("Ошибка при сохранении списка параметров для пользователя {Name}: {Err}", user.Login, res.ErrorResult);
                }
            }

            // Принудительная перезагрузка сессии пользователя: приведет к обновлению кэша безопасности, если этот пользователь уже в системе
            await _securityDataManager.ResetUserSessionAsync(user.Login);
        }
    }
}
