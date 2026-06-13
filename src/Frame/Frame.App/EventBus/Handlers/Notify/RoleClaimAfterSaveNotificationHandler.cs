using Frame.App.EventBus.Events.Notify;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;
using MediatR;

namespace Frame.App.EventBus.Handlers.Notify
{
    public class RoleClaimAfterSaveNotificationHandler(
        IUserSecurityDataManager securityDataManager) 
        : INotificationHandler<EntityAfterSaveNotification<RoleClaim>>
    {
        private readonly IUserSecurityDataManager _securityDataManager = 
            securityDataManager ?? throw new ArgumentNullException(nameof(securityDataManager));

        public async Task Handle(EntityAfterSaveNotification<RoleClaim> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity == null)
            {
                return;
            }

            RoleClaim entity = notification.Entity;
            if (entity.Role != null)
            {
                await _securityDataManager.ResetRoleSessionsAsync(entity.Role);
            }
        }
    }
}
