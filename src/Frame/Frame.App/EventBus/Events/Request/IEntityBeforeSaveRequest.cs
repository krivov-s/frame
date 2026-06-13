using Frame.App.EventBus.Core;
using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;

namespace Frame.App.EventBus.Events.Request
{
    public interface IEntityBeforeSaveRequest : IAppRequest
    {
        public IObjectStorage ObjectStorage { get; }
        public BaseEntity Entity { get; }
        public EntityStorageState EntityStorageState { get; }
    }
}
