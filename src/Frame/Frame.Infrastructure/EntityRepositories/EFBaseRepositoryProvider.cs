//
// namespace Frame.Infrastructure.EntityRepositories
// {
//     //public class EFBaseRepositoryProvider(IServiceProvider services, AppDbContext dbContext, IAppCore appCore) : IBaseRepositoryProvider
//     //{
//     //    private readonly AppDbContext _dbContext = dbContext;
//     //    private readonly IServiceProvider _services = services;
//     //    private readonly IAppCore _appCore = appCore;
//     //    /// <summary>
//     //    /// Создание нового EFBaseRepository репозитория или получение из DI специфичного репозитория, 
//     //    /// если таковой был зарегистрирован на этапе конфигурирования конвейера приложения.
//     //    /// </summary>
//     //    /// <typeparam name="TEntity"></typeparam>
//     //    /// <param name="requestNewConnection">true - если требуется создание нового соединения с БД (нового экземпляра контекста)</param>
//     //    /// <returns>Готовый сконфигурированный экземпляр контекста</returns>
//     //    /// <exception cref="Exception"></exception>
//     //    public IBaseRepository<TEntity> GetBaseRepository<TEntity>(bool requestNewConnection = false) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
//     //    {
//     //        // Если требуется новый контекст - запрашиваем его
//     //        AppDbContext? dbContext = requestNewConnection == true ? _services.GetService<AppDbContext>() : _dbContext;
//     //        if (dbContext == null)
//     //        {
//     //            throw new Exception("Ошибка получения сервиса AppDbContext из DI");
//     //        }
//
//     //        // Сначала пытаемся получить из DI специфичный валидатор для заданного типа. Если его нет - используем базовый.
//     //        IValidator<TEntity>? validator = _services.GetService<IValidator<TEntity>>();
//     //        validator ??= new BaseEntityValidator<TEntity>();
//
//     //        // Сначала пытаемся получить из DI специфичный репозиторий для заданного типа. Если его нет - используем базовый.
//     //        IBaseRepository<TEntity>? repository = _services.GetService<IBaseRepository<TEntity>>();
//     //        if (repository == null)
//     //        {
//     //            // Используем базовый репозиторий
//     //            ILogger<EFBaseRepository<TEntity>>? _logger = _services.GetService<ILogger<EFBaseRepository<TEntity>>>();
//     //            repository = new EFBaseRepository<TEntity>(dbContext, _appCore, _logger, validator);
//     //            return repository;
//     //        }
//     //        else
//     //        {
//     //            // Используем специфичный репозиторий.
//     //            // Все специфичные репозитории необходимо дополнительно донастроить через метод Configure
//     //            Dictionary<int, object> args = new()
//     //            {
//     //                { (int)EFBaseRepositoryParams.ObjectStorage, dbContext },
//     //            };
//     //            if (validator != null) args.Add((int)EFBaseRepositoryParams.Validator, validator);
//
//     //            repository.Configure(args);
//     //        }
//     //        // По-умолчанию 
//     //        return repository;
//     //    }
//     //}
// }
