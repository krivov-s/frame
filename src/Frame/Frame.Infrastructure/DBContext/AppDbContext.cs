using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Frame.App.Cores;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.DBContext
{
    public partial class AppDbContext
    {
        public static int ContextCount;
        private DbContextOptions<AppDbContext> _options;
        private ILogger<AppDbContext> _logger;

        /// <summary>
        /// В конвейер asp.net core необходимо заранее включить сервис ICurrentUserService, в котором должна быть реализована 
        /// возможность получить текущего пользователя.
        /// Через DI этот экземпляр будет каждый раз подставляться при создании контекста.
        /// Реализация данного сервиса в перспективе может зависеть от того, в какой среде (инфраструктуре) будет выполняться сервис.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="userCore">Интерфейс, который используется для получения данных безопасности текущего пользователя.</param>
        /// <param name="serverCore"></param>
        /// <param name="logger">Просто логгер</param>
        public AppDbContext(DbContextOptions<AppDbContext> options,
                            IUserCore userCore, 
                            IServerCore serverCore,
                            ILogger <AppDbContext> logger) : base(options)       
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(userCore);
            ArgumentNullException.ThrowIfNull(serverCore);
            ArgumentNullException.ThrowIfNull(logger);
            _options = options;
            _userCore = userCore;
            _logger = logger;
            ContextCount++;
        }

        /// <summary>
        /// Метод возвращает true, если в контексте есть хотя бы одно значимое изменение (Add/Modify/Delete)
        /// </summary>
        /// <returns></returns>
        public bool IsDirty()
        {
            ChangeTracker.DetectChanges();
            return base.ChangeTracker.Entries().Any(e => e.State != EntityState.Detached && e.State != EntityState.Unchanged);
        }

//         protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//         {
// #if DEBUG
//             optionsBuilder.LogTo(message => Debug.WriteLine(message));
//             optionsBuilder.EnableSensitiveDataLogging();
// #endif      
//             base.OnConfiguring(optionsBuilder);
//
//             optionsBuilder.LogTo(message => _logger?.LogInformation("{Message}", message), LogLevel.Information);
//         }
//
        /// <summary>
        /// Контекст работает с MSSql? 
        /// </summary>
        /// <returns></returns>
        public bool IsSqlServer() => Database.ProviderName?.ToLower().Contains(".sqlserver") ?? false;
        
        /// <summary>
        /// Контекст работает с Postgres? 
        /// </summary>
        /// <returns></returns>
        public bool IsPostgres() => Database.ProviderName?.ToLower().Contains("postgre") ?? false;
        
        /// <summary>
        /// Помимо стандартного сохранения изменений в БД метод осуществляет вызов OnBefore/OnAfterSave и OnBefore/OnAfterDelete для каждого объекта.
        /// В случае, если не проходит проверка - выбрасываем FrameEntityException.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            CheckAddModifyDeleteRights();

            // OnBeforeSaveEntities(); // => проверки логики выполняются на уровне репозиториев

            // InitEntitiesList();

            IEnumerable<Tuple<EntityEntry, AuditRecord>> temoraryAuditEntities = [];

            if (AuditMode)
            {
                temoraryAuditEntities = await AuditNonTemporaryProperties();
            }

            //await CallOnBeforeSaveAsync();
            //await CallOnBeforeDeleteAsync();

            int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (AuditMode)
            {
                await AuditTemporaryProperties(temoraryAuditEntities);
            }

            //await CallOnAfterSaveAsync();
            //await CallOnAfterDeleteAsync();

            return result;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            _logger.LogError("Необходимо всю запись выполнять через SaveChangesAsync!");
            throw new NotImplementedException("Необходимо всю запись выполнять через SaveChangesAsync!");
        }
    }
}
