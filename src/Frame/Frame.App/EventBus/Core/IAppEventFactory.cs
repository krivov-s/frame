using Frame.App.EventBus.Events.Notify;
using Frame.App.EventBus.Events.Request;
using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;

namespace Frame.App.EventBus.Core
{
    public interface IAppEventFactory
    {
        public void RegisterEntityBeforeSaveScriptRequest<TEntity>() where TEntity : BaseEntity;
        public void RegisterEntityAfterSaveNotification<TEntity>() where TEntity : BaseEntity;

        public IEntityBeforeSaveRequest? CreateBeforeSaveRequest(BaseEntity entity, 
                                                                 IObjectStorage storage, 
                                                                 EntityStorageState entityStorageState);
        public IEntityAfterSaveNotification? CreateAfterSaveNotification(BaseEntity entity, 
                                                                         IObjectStorage storage, 
                                                                         EntityStorageState entityStorageState);
    }
}
