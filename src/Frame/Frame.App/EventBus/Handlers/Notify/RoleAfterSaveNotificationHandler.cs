using Frame.App.EventBus.Events.Notify;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Handlers.Notify
{
    public class RoleAfterSaveNotificationHandler(
        ILogger<RoleAfterSaveNotificationHandler> logger, 
        IUserSecurityDataManager securityDataManager) : INotificationHandler<EntityAfterSaveNotification<Role>>
    {
        private readonly ILogger<RoleAfterSaveNotificationHandler> _logger = 
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IUserSecurityDataManager _securityDataManager = 
            securityDataManager ?? throw new ArgumentNullException(nameof(securityDataManager));

        public async Task Handle(EntityAfterSaveNotification<Role> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity == null)
            {
                _logger.LogError("В {handler} передан нулевой объект!", nameof(RoleAfterSaveNotificationHandler));
                return;
            }

            Role role = notification.Entity;
            
            // Принудительная перезагрузка сессии пользователей для данной роли
            await _securityDataManager.ResetRoleSessionsAsync(role);
        }
    }
}
