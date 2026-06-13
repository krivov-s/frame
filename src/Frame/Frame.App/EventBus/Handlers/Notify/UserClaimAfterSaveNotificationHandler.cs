using Frame.App.EventBus.Events.Notify;
using Frame.Domain.Entities.Core.Security;
using Frame.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Handlers.Notify
{
    public class UserClaimAfterSaveNotificationHandler(ILogger<UserClaimAfterSaveNotificationHandler> logger) 
        : INotificationHandler<EntityAfterSaveNotification<UserClaim>>
    {
        public Task Handle(EntityAfterSaveNotification<UserClaim> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity == null)
            {
                return Task.FromResult(Result.Error("Передан нулевой объект!!!"));
            }

            UserClaim entity = notification.Entity;
            // logger.LogInformation("Событие после сохранения UserClaim {name}", entity.Key);

            return Task.FromResult(Result.Success);
        }
    }
}
