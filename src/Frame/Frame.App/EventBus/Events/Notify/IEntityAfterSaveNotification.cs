using Frame.App.EventBus.Core;
using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;

namespace Frame.App.EventBus.Events.Notify
{
    public interface IEntityAfterSaveNotification : IAppNotification
    {
        public IObjectStorage ObjectStorage { get; }
        public BaseEntity Entity { get; }
        public EntityStorageState EntityStorageState { get; }
        
        // public bool IsNew { get; }
        // public bool IsDeleted { get; }
    }
}
