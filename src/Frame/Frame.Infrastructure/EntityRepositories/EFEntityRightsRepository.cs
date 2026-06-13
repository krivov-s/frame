using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Frame.App.IEntityRepositories;
using Frame.App.Security;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.EntityRepositories
{
    /// <summary>
    /// После успешного сохранения/изменения/удаления экземпляра TEntityRights необходимо в UserCore сбросить текущий кэш 
    /// с настройками безопасности для каждого пользователя, у которого есть роль, на которую ссылается экземпляр TEntityRights.
    /// Именно для этого и реализован отдельный репозиторий.
    /// </summary>
    /// <returns></returns>
    public class EFEntityRightsRepository(IObjectStorage objectStorage,
                                          IUserSecurityDataManager userSecurityDataManager,
                                          ILogger<EFBaseRepository<TEntityRights>>? logger,
                                          IValidator<TEntityRights>? validator = null) :
                    EFBaseRepository<TEntityRights>(objectStorage, logger, validator)
    {
        private readonly IUserSecurityDataManager _userSecurityDataManager = userSecurityDataManager;
        //protected IUserCore _userCore = userCore;

        /// <summary>
        /// В данном перегруженном методе <strong>будет вызван метод <see cref="IObjectStorage.SaveChangesAsync"/>!</strong>, 
        /// а затем будет произведена очистка кэша безопасности пользователей
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override async Task<Result> AddAsync(TEntityRights entity)
        {
            Result result = await _objectStorage.AddAndSaveAsync(entity);
            if (!result.IsError)
            {
                if (!result.IsError)
                {
                    await ResetUserSessions(entity);
                }
            }
            return result;
        }

        /// <summary>
        /// В данном перегруженном методе <strong>будет вызван метод <see cref="IObjectStorage.SaveChangesAsync"/>!</strong>, 
        /// а затем будет произведена очистка кэша безопасности пользователей
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override async Task<Result> UpdateAsync(TEntityRights entity)
        {
            Result result = await _objectStorage.UpdateAndSaveAsync(entity);
            if (!result.IsError)
            {
                if (!result.IsError)
                {
                    await ResetUserSessions(entity);
                }
            }
            return result;
        }

        /// <summary>
        /// В данном перегруженном методе <strong>будет вызван метод <see cref="IObjectStorage.SaveChangesAsync"/>!</strong>, 
        /// а затем будет произведена очистка кэша безопасности пользователей
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override async Task<Result> DeleteAsync(TEntityRights entity)
        {
            Result result = await _objectStorage.DeleteAndSaveAsync(entity);
            if (!result.IsError)
            {
                if (!result.IsError)
                {
                    return await ResetUserSessions(entity);
                }
            }
            return result;
        }

        private async Task<Result> ResetUserSessions(TEntityRights entity)
        {
            string strErrorTemplate =   "Ошибка при сбросе кэша UserSession в UserCore для пользователей, " +
                                        "для которых должны были измениться права после пересохранения TEntityRights ";

            try
            {
                // Получаем список всех пользователей с заданным RoleId
                if (entity != null && entity.RoleId != 0)
                {
                    Result<IQueryable<User>> resQuery = _objectStorage.GetQuery<User>();
                    if (resQuery.IsError || resQuery.Value == null)
                    {
                        string strError = $"{strErrorTemplate} при получении списка пользователей: {resQuery.ErrorResult}";
                        _logger.LogSecurity(strError);
                        return Result.Error(strError);
                    }
                    Result<IQueryable<UsersRoles>> resQueryRoles = _objectStorage.GetQuery<UsersRoles>();
                    if (resQueryRoles.IsError || resQueryRoles.Value == null)
                    {
                        string strError = $"{strErrorTemplate} при получении списка ролей: {resQuery.ErrorResult}";
                        _logger.LogSecurity(strError);
                        return Result.Error(strError);
                    }

                    IQueryable<User> query = resQuery.Value;
                    IQueryable<UsersRoles> queryUsersRoles = resQueryRoles.Value;
                    List<string> userLogins = await query.Where(u => queryUsersRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == entity.RoleId))
                                                              .Select(u => u.Login)
                                                              .ToListAsync();
                    string errorResult = "";
                    foreach (string login in userLogins)
                    {
                        Result res = await _userSecurityDataManager.ResetUserSessionAsync(login);
                        if (res.IsError)
                        {
                            errorResult += res.ErrorResult + "; ";
                        }
                    }
                    if (errorResult.Length > 0)
                    {
                        string strError = $"{strErrorTemplate}: {errorResult}";
                        _logger.LogSecurity(strError);
                        return Result.Error(strError);
                    }
                    return Result.Success;
                }
                else
                {
                    string strError = $"{strErrorTemplate}: передан нулевой объект {nameof(TEntityRights)}";
                    _logger.LogSecurity(strError);
                    return Result.Error(strError);
                }
            }
            catch (Exception ex) 
            {
                string strError = $"{strErrorTemplate}: {ex.Message}";
                _logger.LogSecurity(strError);
                _logger.LogError(ex, "{strError}", strError);
                return Result.Error(strError);
            }
        }

    }
}
