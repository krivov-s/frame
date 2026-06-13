//
// namespace Frame.Domain.EntityValidators
// {
//
//     ///// <summary>
//     ///// Синглтон, который при создании конструирует карту {Тип объекта, Тип валидатора} и предоставляет методы получения экземпляров валидаторов для заданных типов объектов.
//     ///// <para>Предварительно все валидаторы должны были быть зарегистрированы в DI с помощью extension-метода <see cref="AddBaseEntityValidators"/></para>
//     ///// </summary>
//     //public class EntityValidatorProvider
//     //{
//     //    //protected IServiceProvider _serviceProvider;
//
//     //    /// <summary>
//     //    /// Коллекция {Тип объекта, Тип валидатора}
//     //    /// </summary>
//     //    private static readonly Dictionary<Type, Type> _validatorTypes = [];
//     //    private object lockMap = new();
//
//     //    public EntityValidatorProvider(Dictionary<Type, Type> validatorTypesMap)
//     //    {
//     //        lock(lockMap)
//     //        {
//     //            if(_validatorTypes.Count == 0)
//     //            {
//     //                foreach (KeyValuePair<Type, Type> pair in validatorTypesMap)
//     //                {
//     //                    _validatorTypes.Add(pair.Key, pair.Value);
//     //                }
//     //            }
//     //        }
//     //    }
//     //    //public EntityValidatorProvider(IServiceProvider serviceProvider)
//     //    //{
//     //    //    _serviceProvider = serviceProvider;
//
//     //    //    // 1. Сначала заполняем коллекцию специфичными валидаторами, зарегистрированными для конкретных типов 
//     //    //    // Получаем все зарегистрированные сервисы
//     //    //    var descriptors = serviceProvider.GetRequiredService<IServiceCollection>();
//
//     //    //    // Ищем все регистрации IValidator<T>
//     //    //    _validatorTypes = descriptors
//     //    //        .Where(d => d.ServiceType.IsGenericType &&
//     //    //                   d.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>))
//     //    //        .ToDictionary(
//     //    //            d => d.ServiceType.GetGenericArguments()[0],
//     //    //            d => d.ImplementationType ?? d.ServiceType
//     //    //        );
//
//     //    //    // 2. Теперь для каждого типа, наследованного от BaseEntity (кроме BaseGenericEntity), проверяем наличие в коллекции валидатора
//     //    //    // Если есть - пропускаем, если нет - создаем базовый валидатор
//     //    //    foreach (Type tEntity in EntityMetadata.GetAllEntitiesClassTypes())
//     //    //    {
//     //    //        bool isValidatorExists = _validatorTypes.ContainsKey(tEntity);
//     //    //        if (!isValidatorExists)
//     //    //        {
//     //    //            Type tValidatorInterface = typeof(IValidator<>).MakeGenericType(tEntity);
//     //    //            _validatorTypes.Add(tEntity, tValidatorInterface);
//     //    //        }
//     //    //    }
//     //    //}
//
//     //    /// <summary>
//     //    /// Сначала метод пытается получить из DI специфичный валидатор для заданного типа. Если его нет - создает и возвращает базовый валидатор <see cref="BaseEntityValidator{TEntity}"/>.
//     //    /// </summary>
//     //    /// <typeparam name="TEntity"></typeparam>
//     //    /// <returns></returns>
//     //    public IValidator<TEntity>? GetValidator<TEntity>() where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
//     //    {
//     //        return _serviceProvider.GetService<IValidator<TEntity>>();
//     //    }
//
//     //    /// <summary>
//     //    /// Получение валидатора для конкретного экземпляра модели (для типа модели)
//     //    /// </summary>
//     //    /// <param name="entity"></param>
//     //    /// <returns></returns>
//     //    public IValidator? GetValidator(object entity)
//     //    {
//     //        return GetValidator(entity.GetType());
//     //    }
//
//     //    /// <summary>
//     //    /// Получение валидатора из списка валидаторов по типу модели
//     //    /// </summary>
//     //    /// <param name="tEntity"></param>
//     //    /// <returns></returns>
//     //    public IValidator? GetValidator(Type tEntity)
//     //    {
//     //        if (_validatorTypes.TryGetValue(tEntity, out var validatorType))
//     //        {
//     //            object? validator = _serviceProvider.GetService(validatorType);
//     //            return (validator == null) ? null : validator as IValidator;
//     //        }
//     //        return null;
//     //    }
//
//     //}
// }


//using FluentValidation;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Frame.Domain.EntityValidators;
//using Frame.Infrastructure.DBContext;
//using Frame.Domain.Entities.Core;
//using Frame.App.IEntityRepositories;
//using Frame.Domain;

//namespace Frame.Infrastructure.EntityRepositories
//{
//    //public class EFBaseRepositoryProvider(IServiceProvider services, AppDbContext dbContext, IAppCore appCore) : IBaseRepositoryProvider
//    //{
//    //    private readonly AppDbContext _dbContext = dbContext;
//    //    private readonly IServiceProvider _services = services;
//    //    private readonly IAppCore _appCore = appCore;
//    //    /// <summary>
//    //    /// Создание нового EFBaseRepository репозитория или получение из DI специфичного репозитория, 
//    //    /// если таковой был зарегистрирован на этапе конфигурирования конвейера приложения.
//    //    /// </summary>
//    //    /// <typeparam name="TEntity"></typeparam>
//    //    /// <param name="requestNewConnection">true - если требуется создание нового соединения с БД (нового экземпляра контекста)</param>
//    //    /// <returns>Готовый сконфигурированный экземпляр контекста</returns>
//    //    /// <exception cref="Exception"></exception>
//    //    public IBaseRepository<TEntity> GetBaseRepository<TEntity>(bool requestNewConnection = false) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
//    //    {
//    //        // Если требуется новый контекст - запрашиваем его
//    //        AppDbContext? dbContext = requestNewConnection == true ? _services.GetService<AppDbContext>() : _dbContext;
//    //        if (dbContext == null)
//    //        {
//    //            throw new Exception("Ошибка получения сервиса AppDbContext из DI");
//    //        }

//    //        // Сначала пытаемся получить из DI специфичный валидатор для заданного типа. Если его нет - используем базовый.
//    //        IValidator<TEntity>? validator = _services.GetService<IValidator<TEntity>>();
//    //        validator ??= new BaseEntityValidator<TEntity>();

//    //        // Сначала пытаемся получить из DI специфичный репозиторий для заданного типа. Если его нет - используем базовый.
//    //        IBaseRepository<TEntity>? repository = _services.GetService<IBaseRepository<TEntity>>();
//    //        if (repository == null)
//    //        {
//    //            // Используем базовый репозиторий
//    //            ILogger<EFBaseRepository<TEntity>>? _logger = _services.GetService<ILogger<EFBaseRepository<TEntity>>>();
//    //            repository = new EFBaseRepository<TEntity>(dbContext, _appCore, _logger, validator);
//    //            return repository;
//    //        }
//    //        else
//    //        {
//    //            // Используем специфичный репозиторий.
//    //            // Все специфичные репозитории необходимо дополнительно донастроить через метод Configure
//    //            Dictionary<int, object> args = new()
//    //            {
//    //                { (int)EFBaseRepositoryParams.ObjectStorage, dbContext },
//    //            };
//    //            if (validator != null) args.Add((int)EFBaseRepositoryParams.Validator, validator);

//    //            repository.Configure(args);
//    //        }
//    //        // По-умолчанию 
//    //        return repository;
//    //    }
//    //}
//}
