using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;

namespace Frame.App.EventBus.Events.Request
{
    public class EntityBeforeSaveScriptRequest<TEntity>(TEntity entity,
                                                        IObjectStorage objectStorage,
                                                        EntityStorageState entityStorageState)
        : EntityBeforeSaveRequest<TEntity>(entity, objectStorage, entityStorageState) where TEntity : BaseEntity;
}
