using Frame.App.EventBus.Events.Notify;
using Frame.App.FileRepositories;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.FileDocuments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Frame.App.EventBus.Handlers.Notify
{
    public class FileDocumentAfterSaveNotificationHandler(
        ILogger<FileDocumentAfterSaveNotificationHandler> logger,
        IFilesRepository filesRepository)
        : INotificationHandler<EntityAfterSaveNotification<FileDocument>>
    {
        public async Task Handle(EntityAfterSaveNotification<FileDocument> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity == null)
            {
                logger.LogInformation($"FileDocumentAfterSaveNotificationHandler - в сообщении отсутствует объект");
                return;
            }

            if (notification.EntityStorageState == EntityStorageState.Deleted)
            {
                FileDocument entity = notification.Entity;
                logger.LogInformation("Удаляем файл {Key} из хранилища после удаления FileDocument", entity.FileKey);
                if (!string.IsNullOrEmpty(entity.FileKey))
                {
                    await filesRepository.DeleteFileAsync(entity.FileKey);
                }
            }
        }
    }
}
