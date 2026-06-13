using Frame.App.EventBus.Core;
using Frame.App.FileRepositories;
using Frame.App.FrameNotifications;
using Frame.App.IEntityLoaders;
using Frame.App.IEntityRepositories;
using Frame.App.Messaging;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;
using Frame.Infrastructure.DBContext;
using Frame.Infrastructure.EntityLoaders;
using Frame.Infrastructure.EntityRepositories;
using Frame.Infrastructure.EventBus;
using Frame.Infrastructure.FileRepositories;
using Frame.Infrastructure.FrameNotifications;
using Frame.Infrastructure.Messaging;
using Frame.Infrastructure.Security;
using Frame.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Настройка всех необходимых сервисов для инфраструктуры.
        /// Объявление классов с настройками (Settings) находятся в соответствующих разделах (DBContext, FileRepositories).
        /// Настройки должны быть определены на самом верхнем уровне и переданы как аргументы метода. 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="dbSettings">Настройки для БД</param>
        /// <param name="fsType">Настройки для файл-сервера</param>
        /// <param name="logger">Логгер</param>
        /// <param name="migrationAssembly"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IServiceCollection AddFrameInfrastructureServices( this IServiceCollection services, 
                                                                              DatabaseSettings dbSettings,
                                                                              string fsType,
                                                                              ILogger? logger = null,
                                                                              string migrationAssembly = "")
        {
            logger?.LogInformation("Конфигурирование FrameInfrastructureServices...");
            
            // Здесь убедимся, что загружены все сборки уровня Infrastructure
            ServiceTools.LoadAssembliesToApplication("*.Infrastructure.dll");

            DependencyInjection.ConfigureDatabaseServices(services, dbSettings, migrationAssembly);
            DependencyInjection.ConfigureMainServices(services);
            DependencyInjection.ConfigureFileServices(services, fsType);
            
            logger?.LogInformation("Конфигурирование FrameInfrastructureServices успешно завершено");
            
            return services;
        }
        
        private static void ConfigureDatabaseServices(IServiceCollection services, DatabaseSettings dbSettings, string migrationAssembly = "")
        {
            string dbServerType = dbSettings.DbServerType;
            if (dbServerType.Length == 0)
            {
                throw new ArgumentException($"В конфигурации не задано значение {nameof(dbSettings.DbServerType)}");
            }
            
            if(dbServerType != DatabaseSettings.DbTypeSql 
               && dbServerType != DatabaseSettings.DbTypePostgres)
                throw new ArgumentException($"Указано неизвестное значение {nameof(dbSettings.DbServerType)} = " +
                                            $"{dbServerType}. Допустимо: " +
                                            $"{DatabaseSettings.DbTypeSql}, {DatabaseSettings.DbTypePostgres}");

            string connectionString = dbSettings.ResultConnectionString;
            if (connectionString.Length == 0)
            {
                throw new ArgumentException($"В конфигурации не задано значение {nameof(dbSettings.ResultConnectionString)}");
            }


            services.AddDbContext<IAppDbContext, AppDbContext>(options =>
                {
                    if (dbServerType == DatabaseSettings.DbTypeSql)
                    {
                        throw new FrameException("Поддержка DbTypeSql убрана из проекта.");
                    }
                    else
                    {
                        // TODO: при версии Postgres 14 и выше - убрать совместимость
                        options
                            .UseLazyLoadingProxies(false)
                            .UseNpgsql(connectionString, o => o.SetPostgresVersion(12, 0));
                    }
                }
                // .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) 
                ,ServiceLifetime.Transient
                ,ServiceLifetime.Transient);

            if (dbServerType == DatabaseSettings.DbTypeSql)
            {
                throw new FrameException("Поддержка DbTypeSql убрана из проекта.");
            }
            else
            {
                services.AddDbContext<NoSecurityDbContext>(options => 
                        options.UseNpgsql(connectionString,  o =>
                        {
                            o.SetPostgresVersion(12, 0);
                            if(migrationAssembly.Length > 0)
                            {
                                o.MigrationsAssembly(migrationAssembly);
                            }
                        })
                    ,ServiceLifetime.Transient
                    ,ServiceLifetime.Transient);

                services.AddDbContextFactory<AppDbContext>(
                    options => options.UseNpgsql(connectionString, o => o.SetPostgresVersion(12, 0))
                    ,ServiceLifetime.Transient);

                services.AddDbContextFactory<NoSecurityDbContext>(
                    options => options.UseNpgsql(connectionString, o => o.SetPostgresVersion(12, 0))
                    ,ServiceLifetime.Transient);
            }
        }

        public static void ConfigureMainServices(IServiceCollection services)
        {
            services.AddScoped<IUserLoader, UserLoader>();
            services.AddScoped<IRoleLoader, RoleLoader>();
            services.AddScoped<ISMTPSendMessage, SMTPSendMessage>();
            services.AddScoped<IFrameNotificationSender, FrameNotificationSender>();
            services.AddScoped<IUserSecurityDataManager, UserSecurityDataManager>();

            // IEntityScriptLoader используется ScriptCore для загрузки скриптов в обход Security,
            // вне зависимости от текущего пользователя.
            services.AddTransient<IEntityScriptLoader, EntityScriptLoader>();

            // Сервис получения объекта текущего пользователя из БД
            services.AddScoped<IGetCurrentUserService, GetCurrentUserService>();

            // Сервис получения системных настроек
            services.AddScoped<IFrameSettingsLoader, FrameSettingsLoader>();
            
            // Сервис сохранения в БД записи об аудите
            services.AddTransient<IAuditService, AuditService>();
            
            // ======================================================================================
            // Репозитории объектов бизнес-логики из слоя Domain
            // ======================================================================================
            // 1. Универсальный поставщик репозиториев объектных хранилищ.
            //    Альтернативный метод получения новых объектных хранилищ - прямой запрос от services
            services.AddScoped<IObjectStorageProvider, EFObjectStorageProvider>();

            // 2. EFObjectStorage - это обертка вокруг AppDbContext. Регистрируем с областью видимости Transient, аналогично самому контексту
            services.AddTransient<IObjectStorage, EFObjectStorage>();

            // 3. Все репозитории, специфичные для отдельных типов регистрируются отдельно
            services.AddTransient<IBaseRepository<User>, EFUserRepository>();
            services.AddTransient<IBaseRepository<Role>, EFRoleRepository>();
            services.AddTransient<IUserProfileRepository, EfUserProfileRepository>();
            services.AddTransient<IBaseRepository<TEntityRights>, EFEntityRightsRepository>();

            services.AddBaseRepository<UsersRoles>(options => options.ConfigureQuerySpecification = (qs) => qs.Include(ur => ur.User).Include(ur => ur.Role).Build());
            services.AddBaseRepository<UserClaim>(options => options.ConfigureQuerySpecification = (qs) => qs.Include(ur => ur.User!).Build());
            services.AddBaseRepository<RoleClaim>(options => options.ConfigureQuerySpecification = (qs) => qs.Include(ur => ur.Role!).Build());

            // Объект, содержащий кэш залогиненных пользователей со всеми настройками системы безопасности
            // включая Role, Claim, UserSession. Через 
            services.AddSingleton<IUserSecurityData, UserSecurityData>();

            services.AddSingleton<IEventQueue<IAppNotification>,InMemoryMessageQueue<IAppNotification>>();
            
            // Объект, содержащий кэш залогиненных пользователей со всеми настройками системы безопасности
            // включая Role, Claim, UserSession. Через 
            services.AddSingleton<IUserSecurityData, UserSecurityData>();

            services.AddSingleton<IEventQueue<IAppNotification>,InMemoryMessageQueue<IAppNotification>>();
        }

        private static void ConfigureFileServices(IServiceCollection services, string fsType)
        {
            // 4. Репозиторий доступа к хранилищу файлов
            switch (fsType)
            {
                case FileStorageType.TypeSmb:
                    services.AddScoped<IFilesRepository, EzSmbFileServerRepository>();
                    break;
                case FileStorageType.TypeNetworkFolder:
                    services.AddSingleton<IFilesRepository, FileServerRepository>(); // реализация с обычным файл-сервером
                    break;
                case FileStorageType.TypeS3:
                    services.AddScoped<IFilesRepository, S3FilesRepository>(); // реализация с Yandex Object Storage (AWS S3)
                    break;
                default:
                    throw new FrameException("В настройках задан неизвестный тип файлового хранилища!");
            }

            services.AddScoped<IFileDocumentService, FileDocumentService>();
            
            // Добавляем кэш для временного хранения содержимого файлов в памяти (для фотогалереи)
            services.AddMemoryCache();
        }

    }
}
