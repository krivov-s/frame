using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;
using Frame.App.IEntityLoaders;

namespace Frame.Infrastructure.EntityLoaders
{
    public class RoleLoader(IDbContextFactory<NoSecurityDbContext> dbContextFactory, ILogger<RoleLoader> logger) : IRoleLoader
    {
        private readonly IDbContextFactory<NoSecurityDbContext> _dbContextFactory = dbContextFactory
            ?? throw new ArgumentNullException(nameof(dbContextFactory));
        private readonly ILogger<RoleLoader> _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
        
        private NoSecurityDbContext? _dbContext;

        public async Task<Result<Role>> LoadByIdAsync(int id)
        {
            if (_dbContext == null)
            {
                _dbContext = await _dbContextFactory.CreateDbContextAsync();
            }

            Role? role = await _dbContext.Role.Include(r => r.RoleClaims)
                                               .Include(r => r.UsersRoles)
                                                .ThenInclude(l => l.User)
                                               .Include(r => r.EntityRights)
                                               .Where(r => r.Id == id)
                                               .AsSplitQuery()
                                               .FirstOrDefaultAsync();
            if (role == null)
            {
                string err = $"В БД не зарегистрировано роли с id = {id}";
                _logger.LogError("{Err}", err);
                return Result<Role>.Error(err);
            }

            return Result<Role>.Success(role);
        }

        public async Task<Result<Role>> LoadByNameAsync(string strName)
        {
            if (strName.Length == 0)
            {
                string err = "Не задано имя роли!"; 
                _logger.LogError("{Err}", err);
                return Result<Role>.Error(err);
            }

            if (_dbContext == null)
            {
                _dbContext = await _dbContextFactory.CreateDbContextAsync();
            }
            List<Role> lst = await _dbContext.Role.Include(r => r.RoleClaims)
                                                  .Include(r => r.UsersRoles)
                                                    .ThenInclude(l => l.User)
                                                  .Include(r => r.EntityRights)
                                                  .Where(r => r.Name == strName)
                                                  .AsSplitQuery()
                                                  .ToListAsync();
            if (lst.Count > 1)
            {
                string err = $"В БД зарегистрировано более одной роли с именем {strName}"; 
                _logger.LogError("{Err}", err);
                return Result<Role>.Error(err);
            }
            if (lst.Count == 0)
            {
                string err = $"В БД не зарегистрирована роль с именем {strName}"; 
                _logger.LogError("{Err}", err);
                return Result<Role>.Error(err);
            }

            return Result<Role>.Success(lst[0]);
        }
        public async Task<Result<Role>> LoadByNormalizedNameAsync(string strNormalizedName)
        {
            if (_dbContext == null)
            {
                _dbContext = await _dbContextFactory.CreateDbContextAsync();
            }
            List<Role> lst = await _dbContext.Role.Include(r => r.RoleClaims)
                                                  .Include(r => r.UsersRoles)
                                                    .ThenInclude(l => l.User)
                                                  .Include(r => r.EntityRights)
                                                  .Where(r => r.NormalizedRoleName == strNormalizedName)
                                                  .AsSplitQuery()
                                                  .ToListAsync();

            if (lst.Count > 1)
            {
                string err = $"В БД зарегистрировано более одной роли с именем {strNormalizedName}"; 
                _logger.LogError("{Err}", err);
                return Result<Role>.Error(err);
            }
            if (lst.Count == 0)
            {
                string err = $"В БД не зарегистрирована роль с именем {strNormalizedName}"; 
                _logger.LogError("{Err}", err);
                return Result<Role>.Error(err);
            }

            return Result<Role>.Success(lst[0]);
        }

    }
}
