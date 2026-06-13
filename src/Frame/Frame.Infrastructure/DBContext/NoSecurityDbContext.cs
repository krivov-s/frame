using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Frame.Domain.Entities.Test;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Entities.Metadata;
using Frame.Shared;

namespace Frame.Infrastructure.DBContext
{
    public class NoSecurityDbContext : DbContext, IAppDbContext
    {
        /// <summary>
        /// В конвейер asp.net core необходимо заранее включить сервис ICurrentUserService, в котором должна быть реализована 
        /// возможность получить текущего пользователя.
        /// Через DI этот экземпляр будет каждый раз подставляться при создании контекста.
        /// Реализация данного сервиса в перспективе может зависеть от того, в какой среде (инфраструктуре) будет выполняться сервис.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public NoSecurityDbContext(DbContextOptions<NoSecurityDbContext> options, ILogger<NoSecurityDbContext>? logger = null) : base(options)
        {
            ArgumentNullException.ThrowIfNull(options);
        }

        /// <summary>
        /// Контекст работает с MSSql? 
        /// </summary>
        /// <returns></returns>
        public bool IsSqlServer() => Database.ProviderName?.Contains(".SqlServer") ?? false;
        
        /// <summary>
        /// Контекст работает с Postgres? 
        /// </summary>
        /// <returns></returns>
        public bool IsPostgres() => Database.ProviderName?.Contains(".Postgres") ?? false;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            List<Type> types = EntityMetadata.GetAllEntitiesClassTypes();
            foreach (var type in types)
            {
                modelBuilder.Entity(type);
            }
            
            // Получаем список сборок, в названии которых присутствует имя Infrastructure
            var listAssemblies = ServiceTools.GetAssemblies(".Infrastructure");
            
            foreach (var dll in listAssemblies)
            {
                modelBuilder.ApplyConfigurationsFromAssembly(dll);
            }

            #region Для SQL-сервера принудительно устанавливаем точность decimals 18.2

            if (IsSqlServer())
            {
                foreach (var tt in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var prop in tt.GetProperties())
                    {
                        if (prop.ClrType.Name.ToUpper().Contains("DECIMAL"))
                        {
#if DEBUG
                            Debug.WriteLine("\tMSSQL: Set decimal precision for " + prop.Name);
#endif
                            prop.SetColumnType("decimal(18, 2)");
                        }
                    }
                }
            }
            #endregion
            
            base.OnModelCreating(modelBuilder);
        }

        public IQueryable<T> GetSet<T>() where T : BaseEntity
        {
            return Set<T>();
        }

        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<UsersRoles> UsersRoles { get; set; }
        public DbSet<UserClaim> UserClaim { get; set; }
        public DbSet<RoleClaim> RoleClaim { get; set; }
        public DbSet<TestObject> TestObject { get; set; }
        public DbSet<TEntityRights> TEntityRights { get; set; }
        public DbSet<EntityScript> EntityScript { get; set; }
    }
}
