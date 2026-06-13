using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Params;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.App.Cores
{
    /// <summary>
    /// Играет роль вспомогательного объекта, аггрегирующего в себе все что нужно в runtime для выполнения 
    /// служебных процедур (скриптов, фильтров, проверок безопасности и т.п.)
    /// </summary>
    public class AppCore(ParamList userParamList,
                        ParamList systemParamList,
                        User currentUser,
                        IObjectStorageProvider objectStorageProvider,
                        IServiceProvider services,
                        ILogger<AppCore> logger)
    {
        private readonly IObjectStorageProvider _objectStorageProvider = objectStorageProvider ?? throw new ArgumentNullException(nameof(objectStorageProvider));
        public ParamList SystemParamList { get; } = systemParamList ?? throw new ArgumentNullException(nameof(systemParamList));
        public ParamList UserParamList { get; } = userParamList ?? throw new ArgumentNullException(nameof(userParamList));
        public User CurrentUser { get; } = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        public IServiceProvider Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
        public ILogger<AppCore> Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

        public IObjectStorage CreateObjectStorage()
        {
            Result<IObjectStorage> resStorage = _objectStorageProvider.GetObjectStorage();
            resStorage.CheckAndThrow("AppCore: создание нового ObjectStorage");
            return resStorage.Value!;
        }

        public IBaseRepository<TEntity> CreateRepository<TEntity>() 
            where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            IObjectStorage storage = CreateObjectStorage();
            Result<IBaseRepository<TEntity>> resRepo = storage.GetBaseRepository<TEntity>();
            resRepo.CheckAndThrow($"AppCore: получение репозитория {typeof(TEntity).Name}");
            return resRepo.Value!;
        }
    }
}
