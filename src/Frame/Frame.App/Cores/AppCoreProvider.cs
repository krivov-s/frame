using Frame.App.IEntityRepositories;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Params;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.App.Cores
{
    public class AppCoreProvider(IGetCurrentUserNameService getCurrentUserNameService,
                                IUserSecurityDataManager userSecurityDataManager,
                                IServerCore serverCore,
                                IObjectStorageProvider objectStorageProvider,
                                IServiceProvider services,
                                ILogger<AppCoreProvider> logger, 
                                ILogger<AppCore> loggerAppCore)
    {
        private readonly IGetCurrentUserNameService _getCurrentUserNameService = 
            getCurrentUserNameService ?? throw new ArgumentNullException(nameof(getCurrentUserNameService));
        private readonly IUserSecurityDataManager _userSecurityDataManager = 
            userSecurityDataManager ?? throw new ArgumentNullException(nameof(userSecurityDataManager));
        private readonly IServerCore _serverCore = 
            serverCore ?? throw new ArgumentNullException(nameof(serverCore));
        private readonly IObjectStorageProvider _objectStorageProvider = 
            objectStorageProvider ?? throw new ArgumentNullException(nameof(objectStorageProvider));
        private readonly IServiceProvider _services = 
            services ?? throw new ArgumentNullException(nameof(services));
        private readonly ILogger<AppCoreProvider> _logger = 
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ILogger<AppCore> _loggerAppCore = 
            loggerAppCore ?? throw new ArgumentNullException(nameof(loggerAppCore));
        
        /// <summary>
        /// Асинхронное создание AppCore. 
        /// Получение логина пользователя выполняется асинхронно, по канонам Blazor, через
        /// AuthenticationStateProvider.
        /// Если пользователь еще даже не обращался к UserSession -
        /// сессия будет создана, пользователь загружен из БД.
        /// </summary>
        /// <returns></returns>
        public async Task<Result<AppCore>> GetAppCoreAsync()
        {
            try
            {
                string login = await _getCurrentUserNameService.GetLoginAsync();
                Result<UserSession> res = await _userSecurityDataManager.LoadUserSessionAsync(login);
                return createAppCore(res);
            }
            catch (Exception ex)
            {
                string err = $"Ошибка при асинхронном создании AppCore: {ex.Message}";
                _logger.LogError(ex, err);
                return Result<AppCore>.Error(err, ex);
            }
        }

        /// <summary>
        /// Синхронное создание AppCore сработает только в том случае, если текущий пользователь
        /// уже логинился и его сессия создана и может быть получена через синхронный вызов
        /// <see cref="IUserSecurityDataManager.GetUserSession"/>
        /// Получение логина пользователя выполняется синхронно, с использованием IHttpContextAccessor.
        /// Это нормально сработает в Server-side blazor, но не сработает в WebApp.
        /// </summary>
        /// <returns></returns>
        public Result<AppCore> GetAppCore()
        {
            try
            {
                string login = _getCurrentUserNameService.GetLogin();
                Result<UserSession> res = _userSecurityDataManager.GetUserSession(login);
                return createAppCore(res);
            }
            catch (Exception ex)
            {
                string err = $"Ошибка при синхронном создании AppCore: {ex.Message}";
                _logger.LogError(ex, err);
                return Result<AppCore>.Error(err, ex);
            }
        }
        
        private Result<AppCore> createAppCore(Result<UserSession> res)
        {
            if (res.IsError || res.Value == null)
            {
                return Result<AppCore>.Error(res.ErrorResult);
            }
            UserSession userSession = res.Value;
            User currentUser = userSession.User;
            ParamList userParamList = userSession.UserParamList;
            ParamList systemParamList = _serverCore.SystemParamList;

            AppCore appCore = new(userParamList, 
                systemParamList, 
                currentUser,
                _objectStorageProvider,
                _services,
                _loggerAppCore);
            return Result<AppCore>.Success(appCore);
        }
    }
}
