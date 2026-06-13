using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;

namespace Frame.App.EventBus.Events.Request
{
    public class EntityBeforeSaveRequest<TEntity> : IEntityBeforeSaveRequest where TEntity : BaseEntity
    {
        public TEntity Entity { get; }
        BaseEntity IEntityBeforeSaveRequest.Entity => Entity;

        public IObjectStorage ObjectStorage { get; }

        public EntityStorageState EntityStorageState { get; }
        
        public EntityBeforeSaveRequest(TEntity entity, IObjectStorage objectStorage, EntityStorageState entityStorageState)
        {
            Entity = entity;
            ObjectStorage = objectStorage;
            EntityStorageState = entityStorageState;
        }
    }
}
