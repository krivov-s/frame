using Frame.App.EventBus.Events.Notify;
using Frame.App.Scheduler;
using Frame.App.Scripting;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Handlers.Notify
{
    /// <summary>
    /// При любом изменении существующего ScriptCommand удаляет все сведения о нем из ScriptCore.
    /// </summary>
    public class ScriptCommandAfterSaveNotificationHandler(IScriptCore scriptCore, SchedulerService schedulerService, 
        ILogger<ScriptCommandAfterSaveNotificationHandler> logger): INotificationHandler<EntityAfterSaveNotification<ScriptCommand>>
    {
        public async Task Handle(EntityAfterSaveNotification<ScriptCommand> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity == null)
            {
                return;
            }

            ScriptCommand entity = notification.Entity;

            string prefix = $"Пересохранение ScriptCommand {entity}";
            scriptCore.RemoveScriptCommand(entity.Id);
            logger.LogInformation("{Prefix}: операция удалена из кэша IScriptCore.", prefix);
            
            await schedulerService.ScheduleCommandAsync(entity);
        }
    }
}
