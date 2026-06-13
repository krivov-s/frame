using Frame.App.Security;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;
using Frame.App.IEntityLoaders;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.Security
{
    /// <summary>
    /// Компонент загружает данные о безопасности для заданного пользователя и сохраняет их в 
    /// <see cref="UserSecurityData"/>.
    /// Если сведения о пользователе уже были ранее прочитаны, они просто возвращаются без перезагрузки.
    /// Если запрашиваемый пользователь в <see cref="IUserSecurityData"/> еще не существует - 
    /// <see cref="UserSecurityDataManager"/> считывает его из <see cref="IUserLoader"/> вместе со всеми ролями и кладет в 
    /// <see cref="UserSecurityData"/> для последующего переиспользования.
    /// В момент загрузки сессии пользователя также инициализируется UserCore. Это нужно для того, чтобы в дальнейшем
    /// нормально проходили синхронные вызовы к UserCore для получения списка параметров, текущего пользователя и др. 
    /// </summary>
    public class UserSecurityDataManager(IUserSecurityData userSecurityData, 
                                         IUserLoader userLoader,
                                         IRoleLoader roleLoader,
                                         ILogger<UserSecurityDataManager> logger): IUserSecurityDataManager
    {
        private readonly IUserSecurityData _userSecurityData = userSecurityData ?? throw new ArgumentNullException(nameof(userSecurityData));
        private readonly IUserLoader _userLoader = userLoader ?? throw new ArgumentNullException(nameof(userLoader));
        private readonly IRoleLoader _roleLoader = roleLoader ?? throw new ArgumentNullException(nameof(roleLoader));
        private readonly ILogger<UserSecurityDataManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<Result<UserSession>> LoadUserSessionAsync(string login, bool forceReload = false)
        {
            try
            {
                UserSession? session = (forceReload) ? null : _userSecurityData.GetUserSession(login);
                if (session == null)
                {
                    Result<User?> res = await _userLoader.LoadByNameAsync(login, forceReload);
                    if (res.IsError)
                    {
                        return Result<UserSession>.Error(res.ErrorResult);
                    }
                    if (res.Value != null)
                    {
                        _userSecurityData.InitUserSession(res.Value);
                        session = _userSecurityData.GetUserSession(login);
                    }
                    else
                    {
                        string err =
                            $"Непонятная ошибка получения пользователя {login}: и ошибка не вернулась и объект не загрузился.";
                        _logger.LogError(err);
                        return Result<UserSession>.Error(err);
                    }
                }
                return Result<UserSession>.Success(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Result<UserSession>.Error(ex.Message, ex);
            }
        }

        public Result<UserSession> GetUserSession(string login)
        {
            try
            {
                return Result<UserSession>.Success(_userSecurityData.GetUserSession(login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Result<UserSession>.Error(ex.Message, ex);
            }
        }
        
        public async Task<Result> ResetUserSessionAsync(string login)
        {
            if (string.IsNullOrEmpty(login))
            {
                string err = $"{nameof(ResetUserSessionAsync)}: передан пустой пользователь";
                _logger.LogWarning("{Err}", err);
                _logger.LogSecurity(err);
                return Result.Error(err);
            }
            _logger.LogSecurity($"Поступила команда переинициализации сессии пользователя {login}");
            UserSession? session = _userSecurityData.GetUserSession(login);
            if (session == null)
            {
                // Если сессия в кэше еще не существует - то и нечего обновлять
                _logger.LogSecurity($"Пользователь {login} еще не логинился в систему.");
                return Result.Success;
            }
            else
            {
                Result res = await LoadUserSessionAsync(login, true);
                if (!res.IsError)
                {
                    _logger.LogSecurity($"Сессия пользователя {login} переинициализирована.");
                }
                return res;
            }
        }

        public async Task<Result> ResetRoleSessionsAsync(Role role)
        {
            try
            {
                _logger.LogSecurity($"Поступила команда переинициализации всех сессий пользователей для роли {role.Name}");
                // Перезачитываем роль по Id, чтобы однозначно получить всех пользователей данной роли
                Result<Role> resRole = await _roleLoader.LoadByIdAsync(role.Id);
                resRole.CheckAndThrow("Чтение роли с Id = {role.Id}");
                List<UsersRoles> users = resRole.Value!.UsersRoles;
                int iCount = 0;
                foreach (UsersRoles roleRel in users)
                {
                    string login = roleRel.User?.Login ?? "";
                    Result res = await ResetUserSessionAsync(login);
                    if (res.IsError)
                    {
                        string strError = $"Ошибки при сбросе кэша UserSession в UserCore для пользователей с ролью {role.Name}, " +
                                          $"для которых должны были измениться права после пересохранения TEntityRights: {res.ErrorResult}";
                        _logger.LogError(strError);
                        return Result.Error(strError);
                    }
                    iCount++;
                }
                _logger.LogSecurity($"Обновлен кэш безопасности пользователей роли {role.Name} (всего пользователей: {iCount})");
                return Result.Success;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Result<UserSession>.Error(ex.Message, ex);
            }
        }

        // public Result<SecurityProfile> GetUserSecurityProfile<TEntity>(string login) where TEntity : BaseEntity
        // {
        //     return GetUserSecurityProfile(login, typeof(TEntity).Name);
        // }
        //
        // public Result<SecurityProfile> GetUserSecurityProfile(string login, string entityTypeName)
        // {
        //     if (string.IsNullOrWhiteSpace(login))
        //     {
        //         string err = "Не указан login пользователя";
        //         _logger.LogError(err);
        //         return Result<SecurityProfile>.Error(err);
        //     }
        //
        //     if (string.IsNullOrWhiteSpace(entityTypeName))
        //     {
        //         string err = "Не указан тип пользователя";
        //         _logger.LogError(err);
        //         return Result<SecurityProfile>.Error(err);
        //     }
        //         
        //     Result<UserSession> result = GetUserSession(login);
        //     if (result.IsError)
        //     {
        //         return Result<SecurityProfile>.Error(result.ErrorResult); //, new SecurityProfile()
        //     }
        //
        //     UserSession? userSession = result.Value;
        //     if (userSession == null)
        //     {
        //         string err = "UserService.GetUserSession вернул null, при этом не выдал ошибку!";
        //         _logger.LogSecurity(err, LogLevel.Error);
        //         return Result<SecurityProfile>.Error(err); //, new SecurityProfile()
        //     }
        //
        //     SecurityProfile profile = userSession.GetSecurityProfile(entityTypeName) ?? new();
        //     return profile;
        // }

        
    }
}
