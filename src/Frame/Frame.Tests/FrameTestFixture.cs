using Frame.App.Security;
using Frame.Infrastructure.DBContext;
using Frame.Infrastructure.Security;
using Frame.Tests.Shared;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using FluentAssertions;
using Frame.App;
using Frame.App.Cores;
using Frame.App.IEntityRepositories;
using Frame.Domain;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;
using Frame.Shared;
using Serilog;

namespace Frame.Tests
{
// Collection definition to disable parallelism
    [CollectionDefinition("DisableParallelism", DisableParallelization = true)]
    public class DisableParallelismCollection { }
    
    public class FrameTestFixture : IDisposable
    {
        private readonly DbConnection _connection;

        public IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    _serviceProvider = serviceCollection.BuildServiceProvider();
                    return _serviceProvider;
                }
                else
                {
                    return _serviceProvider;
                }
                // _serviceProvider = serviceCollection.BuildServiceProvider();
                // return _serviceProvider;
            }
        }

        private IServiceProvider? _serviceProvider;
        protected IServiceCollection serviceCollection;

        public FrameTestFixture()
        {
            serviceCollection = new ServiceCollection();

            _connection = new SqliteConnection($"Filename=:memory:");
            _connection.Open();
            // _connection = new SqliteConnection($"DataSource=file::memory:{Guid.NewGuid()}");
            // _connection.Open();  // Открытие нового соединения с уникальной базой данных

            // Настройка Serilog для записи логов в файл
            Log.Logger = new LoggerConfiguration()
                //.WriteTo.Console()
                //.WriteTo.Debug()
                .WriteTo.File("frame_test.log")
                .CreateLogger();
            
            serviceCollection.AddLogging(builder => builder.AddSerilog());            
            
            serviceCollection.AddFrameDomainServices();
            serviceCollection.AddFrameApplicationServices();

            // Из Frame.Infrastructure нам нужны только основные сервисы, БД - своя, отдельно, Файл - не используем
            ConfigureDatabaseServices(serviceCollection);
            Frame.Infrastructure.DependencyInjection.ConfigureMainServices(serviceCollection);
            
            // Сервисы для получения текущего пользователя как testuser 
            ConfigureCurrentUserServices(serviceCollection);
            
            //ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public AppDbContext GetAppDbContext()
        {
            AppDbContext? appDbContext = ServiceProvider.GetService<AppDbContext>();
            return appDbContext ?? throw new NullReferenceException("Ошибка при получении AppDbContext от ServiceProvider");
        }
        
        public NoSecurityDbContext GetNoSecurityDbContext()
        {
            NoSecurityDbContext? noSecurityDbContext = ServiceProvider.GetService<NoSecurityDbContext>();
            return noSecurityDbContext 
                   ?? throw new NullReferenceException("Ошибка при получении NoSecurityDbContext от ServiceProvider");
        }

        public IUserCore GetUserCore()
        {
            IUserCore? userCore = ServiceProvider.GetService<IUserCore>();
            return userCore 
                   ?? throw new NullReferenceException("Ошибка при получении IUserCore от ServiceProvider");
        }

        private void ConfigureDatabaseServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<NoSecurityDbContext>(options => options
                .UseSqlite(_connection)                 // "DataSource=:memory:")
                .UseLazyLoadingProxies(false)
                .LogTo(Console.WriteLine), ServiceLifetime.Transient, ServiceLifetime.Transient);
            
            serviceCollection.AddDbContext<IAppDbContext, AppDbContext>(options => options
                .UseSqlite(_connection)
                .LogTo(Console.WriteLine), ServiceLifetime.Transient);
            
            serviceCollection.AddDbContextFactory<AppDbContext>(options => options
                .UseSqlite(_connection)
                .UseLazyLoadingProxies(false)
                .LogTo(Console.WriteLine));
            
            serviceCollection.AddDbContextFactory<NoSecurityDbContext>(options => options
                .UseSqlite(_connection)
                .UseLazyLoadingProxies(false)
                .LogTo(Console.WriteLine));
        }

        private void ConfigureCurrentUserServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IGetCurrentUserNameService, TestCurrentUserService>();
            serviceCollection.AddScoped<IGetCurrentUserService, GetCurrentUserService>();            
        }
        
        public void CreateInitDatabase()
        {
            NoSecurityDbContext? noSecurityDbContext = ServiceProvider.GetService<NoSecurityDbContext>();
            if (noSecurityDbContext == null)
            {
                throw new Exception("Не удалось получить NoSecurityDbContext от тестового ServiceProvider");
            }

            noSecurityDbContext.Database.EnsureDeleted();
            noSecurityDbContext.Database.EnsureCreated();

            CreateUsers(noSecurityDbContext);
        }

        private void CreateUsers(NoSecurityDbContext dbContext)
        {
            User user_system = new() { Login = User.SystemUserName, NormalizedLogin = User.SystemUserName.ToUpper() };
            User user_admin = new() { Login = "testuser", NormalizedLogin = "TESTUSER" };
            User user_user = new() { Login = "testuser2", NormalizedLogin = "TESTUSER2" };
            user_system.SetPassword("XXX");
            user_admin.SetPassword("XXX");
            user_user.SetPassword("XXX");
            dbContext.User.Add(user_system);
            dbContext.User.Add(user_admin);
            dbContext.User.Add(user_user);
            dbContext.SaveChanges();
        }
        
        public async Task<UserSession> LoginTestuserAsync()
        {
            IUserSecurityDataManager manager = GetUserSecurityManager();
            Result<UserSession> res = await manager.LoadUserSessionAsync("testuser", true);
            res.CheckAndThrow("LoadUserSessionAsync");
            return res.Value!;
        }

        public IUserSecurityDataManager GetUserSecurityManager()
        {
            IUserSecurityDataManager? securityDataManager = ServiceProvider.GetService<IUserSecurityDataManager>();
            if (securityDataManager == null)
            {
                throw new Exception("Не удалось получить IUserSecurityDataManager от тестового ServiceProvider");
            }

            return securityDataManager;
        }

        public User GetTestuser()
        {
            IUserSecurityDataManager manager = GetUserSecurityManager();
            Result<UserSession> res = manager.GetUserSession("testuser");
            res.CheckAndThrow("LoadUserSessionAsync");
            return res.Value!.User;
        }

        public IObjectStorage CreateObjectStorage()
        {
            return ServiceProvider.GetService<IObjectStorage>() ??
                   throw new Exception("Ошибка при получении от ServiceProvider IObjectStorage!");
        }

        
        public IBaseRepository<TEntity> CreateNewRepository<TEntity>() 
            where TEntity  : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            IObjectStorage? objectStorage = ServiceProvider.GetService<IObjectStorage>();
            objectStorage.Should().NotBeNull();
            Result<IBaseRepository<TEntity>> resRepo = objectStorage!.GetBaseRepository<TEntity>();
            resRepo.IsError.Should().BeFalse();
            IBaseRepository<TEntity> repository = resRepo.Value!;
            repository.Should().NotBeNull();
            return repository;
        }
        
        public void Dispose()
        {
            // Освобождаем ресурсы, если необходимо
            _connection.Close();
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        
    }
}
