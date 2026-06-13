using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;
using Frame.App.IEntityRepositories;

namespace Frame.Infrastructure.EntityRepositories
{
    public class EFUserRepository(IObjectStorage objectStorage, 
                                  ILogger<EFBaseRepository<User>>? logger, 
                                  IValidator<User>? validator = null) :
                    EFBaseRepository<User>(objectStorage, logger, validator), IUserRepository
    {
        public async Task<Result<User>> GetUserAsync(int id)
        {
            string strErrorTemplate = "Ошибка при получении пользователя по Id";

            try
            {
                if (id == 0)
                {
                    string strError = $"{strErrorTemplate}: передан нулевой идентификатор";
                    return Result<User>.Error(strError);
                }

                strErrorTemplate = strErrorTemplate + $"={id}";

                if (_objectStorage == null)
                {
                    string strError = $"{strErrorTemplate}: объектное хранилище не инициализировано";
                    return Result<User>.Error(strError);
                }

                Result<IQueryable<User>> resQuery = _objectStorage.GetQuery<User>();
                if (resQuery.IsError || resQuery.Value == null)
                {
                    string strError = $"{strErrorTemplate} при получении списка пользователей: {resQuery.ErrorResult}";
                    _logger.LogSecurity(strError);
                    return Result<User>.Error(strError);
                }

                IQueryable<User> query = resQuery.Value;

                User? user = await query.Include(u => u.UsersRoles)
                                        .ThenInclude(ur => ur.Role)
                                        .ThenInclude(r => r.RoleClaims)
                                        .Include(r => r.UserClaims)
                                        .Include(u => u.Division)
                                        .Include(u => u.UserProfile)
                                        .Where(u => u.Id == id)
                                        .AsSplitQuery()
                                        .FirstOrDefaultAsync();
                if (user == null)
                {
                    string strError = $"{strErrorTemplate}: пользователь не найден в БД";
                    return Result<User>.Error(strError);
                }
                else
                {
                    return Result<User>.Success(user);
                }
            }
            catch (Exception ex)
            {
                string strError = $"{strErrorTemplate}: {ex.Message}";
                _logger.LogError(ex, "{Err}", strError);
                _logger.LogSecurity(strError);
                return Result<User>.Error(strError);
            }
        }
    }
}
