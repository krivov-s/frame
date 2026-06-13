using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using Frame.App.IEntityLoaders;
using Frame.Domain.Entities.Core.Scripting;

namespace Frame.Infrastructure.EntityLoaders
{
    public class EntityScriptLoader(IDbContextFactory<NoSecurityDbContext> dbContextFactory, ILogger<EntityScriptLoader> logger) : IEntityScriptLoader
    {
        private NoSecurityDbContext? _dbContext;
        private readonly IDbContextFactory<NoSecurityDbContext> _dbContextFactory = dbContextFactory;
        private readonly ILogger<EntityScriptLoader> _logger = logger;

        public async Task<Result<List<EntityScript>>> LoadAllAsync()
        {
            try
            {
                if (_dbContext == null)
                {
                    _dbContext = await _dbContextFactory.CreateDbContextAsync();
                }

                List<EntityScript> list = await _dbContext.EntityScript.ToListAsync();
                return Result<List<EntityScript>>.Success(list);
            }
            catch (Exception ex)
            {
                string err = $"Ошибка загрузки списка {nameof(EntityScript)} из БД";
                _logger?.LogError(ex, "{err}", err);
                return Result<List<EntityScript>>.Error(err, ex);
            }
        }
    }
}
