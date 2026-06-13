using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;

namespace Frame.App.EventBus.Events.Notify
{
    public class EntityAfterSaveNotification<TEntity> : IEntityAfterSaveNotification where TEntity : BaseEntity
    {
        public TEntity Entity { get; }
        BaseEntity IEntityAfterSaveNotification.Entity => Entity;

        public IObjectStorage ObjectStorage { get; }
        public EntityStorageState EntityStorageState { get; }

        public EntityAfterSaveNotification(TEntity entity, IObjectStorage objectStorage, EntityStorageState entityStorageState)
        {
            Entity = entity;
            ObjectStorage = objectStorage;
            EntityStorageState = entityStorageState;
        }
    }
}
