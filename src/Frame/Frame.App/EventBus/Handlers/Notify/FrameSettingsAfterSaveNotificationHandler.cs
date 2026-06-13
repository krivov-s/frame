using Frame.App.EventBus.Events.Notify;
using Frame.App.Scripting;
using Frame.Domain.Entities.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Handlers.Notify
{
    public class FrameSettingsAfterSaveNotificationHandler(
        ILogger<FrameSettingsAfterSaveNotificationHandler> logger, 
        IScriptCore scriptCore) : INotificationHandler<EntityAfterSaveNotification<FrameSettings>>
    {
        private readonly ILogger<FrameSettingsAfterSaveNotificationHandler> _logger = 
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IScriptCore _scriptCore = 
            scriptCore ?? throw new ArgumentNullException(nameof(scriptCore));

        public async Task Handle(EntityAfterSaveNotification<FrameSettings> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity == null)
            {
                _logger.LogError("В {handler} передан нулевой объект!", nameof(FrameSettingsAfterSaveNotificationHandler));
                return;
            }

            await _scriptCore.ReloadFrameSettingsAsync();
        }
    }
}
