using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Frame.App.IEntityRepositories;
using Frame.Shared;

namespace Frame.Infrastructure.EntityRepositories
{
    public class EFObjectStorageProvider(IServiceProvider services) : IObjectStorageProvider
    {
        private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

        private ILogger<IObjectStorage>? _logger;

        public Result<IObjectStorage> GetObjectStorage()
        {
            IObjectStorage? storage = _services.GetService<IObjectStorage>();
            if(storage == null)
            {
                string strError = "Ошибка получения IObjectStorage";
                Logger?.LogError(strError);
                return Result<IObjectStorage>.Error(strError);
            }
            else
            {
                return Result<IObjectStorage>.Success(storage);
            }
        }

        protected ILogger? Logger
        {
            get
            {
                _logger ??= _services.GetService<ILogger<IObjectStorage>>();
                return _logger;
            }
        }
    }
}
