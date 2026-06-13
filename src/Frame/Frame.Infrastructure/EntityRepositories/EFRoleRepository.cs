using FluentValidation;
using Microsoft.Extensions.Logging;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;
using Frame.App.IEntityRepositories;

namespace Frame.Infrastructure.EntityRepositories
{
    public class EFRoleRepository(IObjectStorage objectStorage, ILogger<EFBaseRepository<Role>>? logger, IValidator<Role>? validator = null) :
                        EFBaseRepository<Role>(objectStorage, logger, validator), IRoleRepository
    {
        public async Task<Result<Role>> GetRoleAsync(int id)
        {
            string strErrorTemplate = "Ошибка при получении роли по Id";

            try
            {
                if(id == 0)
                {
                    string strError = $"{strErrorTemplate}: передан нулевой идентификатор";
                    return Result<Role>.Error(strError);
                }

                strErrorTemplate = strErrorTemplate + $"={id}";

                Result<Role> resRole = await _objectStorage.GetObjAsync<Role>(spec => spec
                    .Include(u => u.UsersRoles)
                        .ThenInclude(ur => ur.User)
                    .Include(r => r.RoleClaims)
                    .Include(r => r.EntityRights)
                    .Where(u => u.Id == id));

                if (resRole.IsError || resRole.Value == null)
                {
                    string strError = $"{strErrorTemplate}: роль не найдена в БД";
                    return Result<Role>.Error(strError);
                }
                return resRole;
            }
            catch (Exception ex)
            {
                string strError = $"{strErrorTemplate}: {ex.Message}";
                _logger.LogError(ex, "{StrError}", strError);
                _logger.LogSecurity(strError);
                return Result<Role>.Error(strError);
            }
        }
    }
}
