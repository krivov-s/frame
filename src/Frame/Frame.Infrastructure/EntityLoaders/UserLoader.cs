using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using Frame.Domain.Entities.Core.Security;
using Frame.App.IEntityLoaders;

namespace Frame.Infrastructure.EntityLoaders
{
    public class UserLoader(IDbContextFactory<NoSecurityDbContext> dbContextFactory, ILogger<UserLoader> logger) : IUserLoader
    {
        private NoSecurityDbContext? _dbContext;
        private readonly IDbContextFactory<NoSecurityDbContext> _dbContextFactory = dbContextFactory 
            ?? throw new ArgumentNullException(nameof(dbContextFactory));
        private readonly ILogger<UserLoader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        protected IQueryable<User> PrepareQuery(IQueryable<User> query)
        {
            return query.Include(u => u.UsersRoles)
                            .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.EntityRights)
                        .Include(u => u.UsersRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RoleClaims)
                        .Include(r => r.UserClaims)
                        .Include(u => u.UserProfile)
                        .AsSplitQuery();
        }

        public Result<User?> LoadByName(string strName, bool forceReload = false)
        {
            _logger.LogDebug($"=================== UserLoader.LoadByName({strName}) ===================");

            //if (forceReload)
            //{
            //    _dbContext.ReloadAllEntities();
            //}

            if (strName.Length == 0)
            {
                return Result<User?>.Error($"Не задано имя пользователя!");
            }

            if (_dbContext == null || forceReload)
            {
                _dbContext = _dbContextFactory.CreateDbContext();
            }

            List<User> lst = PrepareQuery(_dbContext.User.Where(u => u.Login == strName)).ToList();
            if (lst.Count > 1)
            {
                return Result<User?>.Error($"В БД зарегистрировано более одного пользователя с именем {strName}");
            }
            else if (lst.Count == 0)
            {
                return Result<User?>.Error($"В БД не зарегистрирован пользователь с именем {strName}");
            }

            return Result<User?>.Success(lst[0]);
        }

        public async Task<Result<User?>> LoadByIdAsync(int id, bool forceReload = false)
        {
            if (_dbContext == null || forceReload)
            {
                _dbContext = _dbContextFactory.CreateDbContext();
            }
            User? user = await PrepareQuery(_dbContext.User.Where(u => u.Id == id)).FirstOrDefaultAsync();
            if (user == null)
            {
                return Result<User?>.Error($"В БД не зарегистрирован пользователь с id = {id}");
            }

            return Result<User?>.Success(user);
        }

        public async Task<Result<User?>> LoadByNameAsync(string strName, bool forceReload = false)
        {
            _logger.LogDebug($"=================== UserLoader.LoadByNameAsync({strName}) ===================");

            try
            {
                if (_dbContext == null || forceReload)
                {
                    _dbContext = _dbContextFactory.CreateDbContext();
                }

                List<User> lst = await PrepareQuery(_dbContext.User.Where(u => u.Login == strName)).ToListAsync();
                if (lst.Count > 1)
                {
                    return Result<User?>.Error($"В БД зарегистрировано более одного пользователя с именем {strName}");
                }
                else if (lst.Count == 0)
                {
                    return Result<User?>.Error($"В БД не зарегистрирован пользователь с именем {strName}");
                }

                return Result<User?>.Success(lst[0]);
            }
            catch (Exception ex)
            {
                string strErr = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                _logger.LogError(ex, strErr);
                return Result<User?>.Error(strErr, ex);
            }
        }

        public async Task<Result<User?>> LoadByNormalizedNameAsync(string strNormalizedName, bool forceReload = false)
        {
            _logger.LogDebug($"=================== UserLoader.LoadByNormalizedNameAsync({strNormalizedName}) ===================");
            try
            {
                if (_dbContext == null || forceReload)
                {
                    _dbContext = _dbContextFactory.CreateDbContext();
                }

                List<User> lst = await PrepareQuery(_dbContext.User.Where(u => u.NormalizedLogin == strNormalizedName))
                    .ToListAsync();
                if (lst.Count > 1)
                {
                    return Result<User?>.Error(
                        $"В БД зарегистрировано более одного пользователя с именем {strNormalizedName}");
                }
                else if (lst.Count == 0)
                {
                    return Result<User?>.Error($"В БД не зарегистрирован пользователь с именем {strNormalizedName}");
                }

                return Result<User?>.Success(lst[0]);
            }
            catch (Exception ex)
            {
                string strErr = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                _logger.LogError(ex, strErr);
                return Result<User?>.Error(strErr, ex);
            }
        }
    }
}
