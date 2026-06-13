using System.Linq.Dynamic.Core;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Frame.Domain.EntityValidators;
using Frame.Infrastructure.DBContext;
using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;
using Frame.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;
using Frame.App.Cores;
using Frame.App.EventBus.Core;
using Frame.Domain.Params;
using Frame.Domain.QuerySpec;
using Microsoft.EntityFrameworkCore.Storage;
using MediatR;
using SmartFormat;
using Frame.Domain.Entities.Core.Queries;

namespace Frame.Infrastructure.EntityRepositories
{
    /// <summary>
    /// Обертка вокруг <see cref="DbContext"/>. 
    /// Предназначен для формирования типизированных репозиториев (обертка вокруг <see cref="DbSet{TEntity}"/>) и последующего сохранения изменений в <see cref="DbContext"/> 
    /// с корректным вызовом методов измененых моделей OnBefore/After/Save/Delete/Async
    /// </summary>
    public class EFObjectStorage : IObjectStorage
    {
        private readonly IServiceProvider _services;

        private readonly IAppEventDispatcher _appEventDispatcher;
        private readonly IAppEventFactory _appEventFactory;
        private readonly IEntityValidatorFactory _entityValidatorFactory;
        private readonly ILogger<IObjectStorage> _logger;
        private readonly AppCoreProvider _appCoreProvider;

        private AppDbContext? _dbContext;
        private IDbContextTransaction? _transaction;

        private const string ErrFormatString = "{strError}";
        private const string ErrGettingAppDbContext = "Ошибка получения AppDbContext";

        // private const string ErrStopRecursion = "Рекурсивные вызовы к хранилищу запрещены! " +
        //                                         "Необходимо дождаться завершения предыдущей операции";

        // Используем SemaphoreSlim а не простой lock поскольку внутри lock нельзя использовать await
        private readonly SemaphoreSlim _lockDbContext = new SemaphoreSlim(1, 1);
        private readonly object _lockCollections = new object();
        
        public EFObjectStorage(IServiceProvider services)
        {
            _services = services;

            _appEventDispatcher = _services.GetRequiredService<IAppEventDispatcher>()
                ?? throw new FrameException($"Ошибка получения {nameof(IAppEventDispatcher)}");
            
            _appEventFactory = _services.GetRequiredService<IAppEventFactory>()
                ?? throw new FrameException($"Ошибка получения {nameof(IAppEventFactory)}");
            
            _entityValidatorFactory = _services.GetRequiredService<IEntityValidatorFactory>() 
                ?? throw new FrameException($"Ошибка получения {nameof(IEntityValidatorFactory)}");
            
            _logger = _services.GetRequiredService<ILogger<IObjectStorage>>()
                ?? throw new FrameException($"Ошибка получения {nameof(IObjectStorage)}");
            
            _appCoreProvider = _services.GetRequiredService<AppCoreProvider>()
                ?? throw new FrameException($"Ошибка получения {nameof(AppCoreProvider)}");
        }

        public IBaseRepository<TEntity>? GetBaseRepository<TEntity>(string strErrorMessage) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result<IBaseRepository<TEntity>> result = GetBaseRepository<TEntity>();
            if(result.IsError || result.Value == null)
            {
                _logger.LogError(strErrorMessage, result);
                return null;
            }
            else
            {
                return result.Value;
            }
        }

        /// <summary>
        /// Создание нового EFBaseRepository репозитория или получение из DI специфичного репозитория, 
        /// если таковой был зарегистрирован на этапе конфигурирования конвейера приложения.
        /// Контекст всегда подставляется тот, который был создан при создании EFObjectStorage.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns>Готовый сконфигурированный экземпляр репозитория.</returns>
        public Result<IBaseRepository<TEntity>> GetBaseRepository<TEntity>() where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            try
            {
                if (DbContext == null)
                {
                    _logger.LogError(ErrFormatString, ErrGettingAppDbContext);
                    return Result<IBaseRepository<TEntity>>.Error(ErrGettingAppDbContext);
                }


                IValidator<TEntity>? validator = _entityValidatorFactory.GetValidator<TEntity>();

                // Сначала пытаемся получить из DI специфичный репозиторий для заданного типа. Если его нет - используем базовый.
                IBaseRepository<TEntity>? repository = _services.GetService<IBaseRepository<TEntity>>();
                if (repository == null)
                {
                    // Используем базовый репозиторий
                    ILogger<EFBaseRepository<TEntity>>? repoLogger = _services.GetService<ILogger<EFBaseRepository<TEntity>>>();
                    repository = new EFBaseRepository<TEntity>(this, repoLogger, validator);
                }
                else
                {
                    // Используем специфичный репозиторий.
                    // Все специфичные репозитории необходимо дополнительно донастроить через метод Configure
                    Dictionary<int, object> args = [];
                    args.Add((int)EFBaseRepositoryParams.ObjectStorage, this);
                    if (validator != null) args.Add((int)EFBaseRepositoryParams.Validator, validator);

                    Result r = repository.Configure(args);
                    if (r.IsError)
                    {
                        return Result<IBaseRepository<TEntity>>.Error(r.ErrorResult);
                    }
                }

                return Result<IBaseRepository<TEntity>>.Success(repository);
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка получения IBaseRepository<{typeof(TEntity).Name}>";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result<IBaseRepository<TEntity>>.Error($"{strError}: {userMessage}", ex);
            }
        }

        private Result CheckParams<TEntity>(TEntity? entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            if (DbContext == null)
            {
                string strError = "EFObjectStorage не удалось получить сервис AppDbContext";
                _logger.LogError(ErrFormatString, strError);
                return Result.Error(strError);
            }
            if (entity is null)
            {
                string strError = "В EFObjectStorage передан нулевой объект";
                _logger.LogError(ErrFormatString, strError);
                return Result.Error(strError);
            }
            return Result.Success;
        }

        public IQueryable<TEntity>? GetQueryNR<TEntity>(string strErrPrefix = "", 
            Expression<Func<TEntity, bool>>? filter = null) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result<IQueryable<TEntity>> result = GetQuery(strErrPrefix, filter);
            if (result.IsError || result.Value == null)
            {
                return null;
            }
            else
            {
                return result.Value;
            }
        }

        public Result<IQueryable<TEntity>> GetQuery<TEntity>(string strErrPrefix = "", 
            Expression<Func<TEntity, bool>>? filter = null) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            if (DbContext == null)
            {
                string strError = (strErrPrefix.Length) > 0 ? $"{strErrPrefix}: " : "";
                strError += "EFObjectStorage не удалось получить сервис AppDbContext";
                _logger.LogError(ErrFormatString, strError);
                return Result<IQueryable<TEntity>>.Error(strError);
            }

            try
            {
                IQueryable<TEntity> query = DbContext.GetSet<TEntity>();
                if(query == null)
                {
                    string strError = (strErrPrefix.Length) > 0 ? $"{strErrPrefix}: " : "";
                    strError += "Не удалось получить DbSet от DbContext: вернулся null";
                    _logger.LogError(ErrFormatString, strError);
                    return Result<IQueryable<TEntity>>.Error(strError);
                }

                if (filter != null)
                {
                    query = query.Where(filter);
                }
                return Result<IQueryable<TEntity>>.Success(query.AsSplitQuery());
            }
            catch (Exception ex)
            {
                string strError = (strErrPrefix.Length) > 0 ? $"{strErrPrefix}: " : "";
                strError += $"Ошибка при выполнении DbContext.GetSet()";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result<IQueryable<TEntity>>.Error($"{strError}: {userMessage}", ex);
            }
        }

        public async Task<Result<List<TEntity>>> GetListAsync<TEntity>(
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result<IQueryable<TEntity>> resQ = GetQuery<TEntity>(strErrPrefix);
            if (resQ.IsError || resQ.Value == null)
            {
                return Result<List<TEntity>>.Error(resQ.ErrorResult);
            }
            
            await _lockDbContext.WaitAsync(); // Ждем освобождения ресурса
            try
            {
                var ret = await resQ.Value.ToListAsync();
                return Result<List<TEntity>>.Success(ret);
            }
            catch (Exception ex)
            {
                string strError = (strErrPrefix.Length) > 0 ? $"{strErrPrefix}: " : "";
                strError += $"Ошибка при получении списка объектов {typeof(TEntity).Name}";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result<List<TEntity>>.Error($"{strError}: {userMessage}", ex);
            }
            finally
            {
                _lockDbContext.Release();
            }
        }

        public async Task<Result<List<TEntity>>> GetListAsync<TEntity>(
            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null, 
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result<IQueryable<TEntity>> resQ = GetQuery<TEntity>(strErrPrefix);
            if (resQ.IsError || resQ.Value == null)
            {
                return Result<List<TEntity>>.Error(resQ.ErrorResult, resQ.Exception);
            }

            // С этой проверкой не работает вариант, когда в одном компоненте несколько браузеров!
            // if (_lockDbContext.CurrentCount == 0)
            // {
            //     return Result<List<TEntity>>.Error(ErrStopRecursion);
            // }
            await _lockDbContext.WaitAsync(); // Ждем освобождения ресурса
            try
            {
                IQueryable<TEntity> query = resQ.Value;
                if (querySpec != null)
                {
                    IQuerySpecification<TEntity> spec = new QuerySpecification<TEntity>();
                    spec = querySpec(spec);
                    query = EFHelpers.ApplyQuerySpec(spec, query);
                }
                var ret = await query.ToListAsync();
                return Result<List<TEntity>>.Success(ret);
            }
            catch (Exception ex)
            {
                string strError = (strErrPrefix.Length) > 0 ? $"{strErrPrefix}: " : "";
                strError += $"Ошибка при получении списка объектов {typeof(TEntity).Name}";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result<List<TEntity>>.Error($"{strError}: {userMessage}", ex);
            }
            finally
            {
                _lockDbContext.Release();
            }
        }

        public async Task<Result<List<BaseEntity>>> GetBaseEntityListAsync<TEntity>(
            Func<IQuerySpecification<TEntity>,IQuerySpecification<TEntity>>? querySpec = null,
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result<List<TEntity>> res = await GetListAsync(querySpec, strErrPrefix);
            if (res.IsError || res.Value == null)
            {
                return Result<List<BaseEntity>>.Error(res.ErrorResult);
            }
            
            List<BaseEntity> list = res.Value.Cast<BaseEntity>().ToList();
            return Result<List<BaseEntity>>.Success(list);
        }

        public async Task<Result<List<dynamic>>> GetDynamicListAsync<TEntity>(
            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null, 
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result<IQueryable<TEntity>> res = GetQuery<TEntity>(strErrPrefix);
            if (res.IsError || res.Value == null)
            {
                return Result<List<dynamic>>.Error(res.ErrorResult);
            }
            
            // С этой проверкой не работает вариант, когда в одном компоненте несколько браузеров!
            // if (_lockDbContext.CurrentCount == 0)
            // {
            //     return Result<List<dynamic>>.Error(ErrStopRecursion);
            // }
            await _lockDbContext.WaitAsync(); // Ждем освобождения ресурса
            try
            {
                IQueryable<TEntity> query = res.Value;
                if (querySpec != null)
                {
                    IQuerySpecification<TEntity> spec = new QuerySpecification<TEntity>();
                    spec = querySpec(spec);
                    query = EFHelpers.ApplyQuerySpec(spec, query);
                }

                List<dynamic> result = await query.ToDynamicListAsync();
                return Result<List<dynamic>>.Success(result);
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при получении списка объектов";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result<List<dynamic>>.Error($"{strError}: {userMessage}", ex);
            }
            finally
            {
                _lockDbContext.Release();
            }
        }
        
        public async Task<Result<List<dynamic>>> GetDynamicListAsync(SavedQuery savedQuery, ParamList? paramList = null, string strErrPrefix = "")
        {
            string strError = (strErrPrefix.Length) > 0 ? $"{strErrPrefix}: " : "";

            if (DbContext == null)
            {
                strError += $"Не задан {nameof(DbContext)}";
                _logger.LogError(ErrFormatString, strError);
                return Result<List<dynamic>>.Error(strError);
            }

            if (savedQuery.EntityType == null)
            {
                strError += $"У {nameof(savedQuery)} не определен {nameof(savedQuery.EntityType)}";
                _logger.LogError(ErrFormatString, strError);
                return Result<List<dynamic>>.Error(strError);
            }
            
            // С этой проверкой не работает вариант, когда в одном компоненте несколько браузеров!
            // if (_lockDbContext.CurrentCount == 0)
            // {
            //     return Result<List<dynamic>>.Error(ErrStopRecursion);
            // }
            await _lockDbContext.WaitAsync(); // Ждем освобождения ресурса
            try
            {
                IQueryable? query = DbContext.GetSet(savedQuery.EntityType);
                if (query == null)
                {
                    strError += $"Не удалось получить DbSet для типа {savedQuery.EntityType}";
                    _logger.LogError(ErrFormatString, strError);
                    return Result<List<dynamic>>.Error(strError);
                }

                // Готовим переменные для работы Smart.Format
                Result<Object> resContainer = (paramList != null)
                    ? await SmartFormatHelpers.CreateContainerForSmartFormat(paramList, _appCoreProvider)
                    : await SmartFormatHelpers.CreateContainerForSmartFormat(savedQuery.ParamListJson,
                        _appCoreProvider);

                if (resContainer.IsError || resContainer.Value == null)
                {
                    return Result<List<dynamic>>.Error(resContainer.ErrorResult);
                }

                object container = resContainer.Value;

                if (savedQuery.Select != "")
                {
                    query = query.Select(savedQuery.Select);
                }

                if (savedQuery.Where != "")
                {
                    string where = Smart.Format(savedQuery.Where, container);
                    query = query.Where(where);
                }

                if (savedQuery.OrderBy != "")
                {
                    string orderBy = Smart.Format(savedQuery.OrderBy, container);
                    query = query.OrderBy(orderBy);
                }

                List<dynamic> result = await query.ToDynamicListAsync();
                return Result<List<dynamic>>.Success(result);
            }
            catch (Exception ex)
            {
                strError += $"Ошибка при получении списка объектов по запросу {savedQuery.Name}";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result<List<dynamic>>.Error($"{strError}: {userMessage}", ex);
            }
            finally
            {
                _lockDbContext.Release();
            }
        }

        public async Task<Result<List<dynamic>>> GetDynamicListAsync<TEntity>(
            Expression<Func<TEntity, bool>>? filter = null,
            string strErrPrefix = "")  where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result<IQueryable<TEntity>> resQ = GetQuery(strErrPrefix, filter);
            if (resQ.IsError || resQ.Value == null)
            {
                return Result<List<dynamic>>.Error(resQ.ErrorResult);
            }

            // С этой проверкой не работает вариант, когда в одном компоненте несколько браузеров!
            // if (_lockDbContext.CurrentCount == 0)
            // {
            //     return Result<List<dynamic>>.Error(ErrStopRecursion);
            // }
            await _lockDbContext.WaitAsync(); // Ждем освобождения ресурса
            try
            {
                var res = await resQ.Value.ToDynamicListAsync();
                return Result<List<dynamic>>.Success(res);
            }
            catch (Exception ex)
            {
                string strError = (strErrPrefix.Length) > 0 ? $"{strErrPrefix}: " : "";
                strError += $"Ошибка при получении списка объектов {typeof(TEntity).Name}";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result<List<dynamic>>.Error($"{strError}: {userMessage}", ex);
            }
            finally
            {
                _lockDbContext.Release();
            }
        }
        
        /// <summary>
        /// Если передаваемый объект уже отслеживается контекстом - вызов перенаправляется в <see cref="Update{TEntity}(TEntity)"/>
        /// <para>
        /// Если объект не отслеживается, но его Id != 0 - объект присоединяется к контексту.
        /// ВАЖНО!!! В этом случае контексту принудительно будет сказано, что объект изменен, и его нужно пересохранить.
        /// </para>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Result Add<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result res = CheckParams(entity);
            if (res.IsError) return res;

            if (DbContext!.Entry(entity).State != EntityState.Detached)
            {
                return Update(entity);
            }

            try
            {
                lock (_lockCollections)
                {
                    if (entity.Id > 0)
                    {
                        // Если объект не присоединен к контексту, но его Id > 0 - присоединяем.
                        DbContext.Attach(entity);
                        // И отмечаем что он изменен, что его нужно пересохранить.
                        DbContext.Entry(entity).State = EntityState.Modified;
                    }
                    else
                    {
                        DbContext.Add(entity);
                    }
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при добавлении объекта {typeof(TEntity).Name} в хранилище.";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}");
            }
        }

        public Result Update<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            try
            {
                Result res = CheckParams(entity);
                if (res.IsError) return res;

                if (DbContext!.Entry(entity).State == EntityState.Detached)
                {
                    string err = "Нельзя пытаться обновить в репозитории объект, который не отслеживается контекстом";
                    _logger.LogError("Error: {Err}", err);
                    return Result.Error(err);
                }

                // Если объект отслеживается контекстом, никакие другие действия пока не требуются.
                return Result.Success;
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при обновлении объекта {typeof(TEntity).Name} в хранилище.";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}", ex);
            }
        }

        public Result Delete<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result res = CheckParams(entity);
            if (res.IsError) return res;

            if (DbContext!.Entry(entity).State == EntityState.Detached)
            {
                string err = "Нельзя пытаться удалить их хранилища объект, который не отслеживается";
                _logger.LogError(ErrFormatString, err);
                return Result.Error(err);
            }
            
            try
            {
                lock (_lockCollections)
                {
                    DbContext.Remove(entity);
                    return Result.Success;
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при удалении объекта {typeof(TEntity).Name} из хранилища.";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}", ex);
            }
        }

        public Result Delete<TEntity>(int id) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            if (DbContext == null)
            {
                string err = "EFObjectStorage не удалось получить сервис AppDbContext";
                _logger.LogError(ErrFormatString, err);
                return Result.Error(err);
            }
            if (id == 0)
            {
                string err = "Передан нулевой идентификатор объекта для удаления";
                _logger.LogError(ErrFormatString, err);
                return Result.Error(err);
            }

            try
            {
                EntityEntry<TEntity>? ee = DbContext.ChangeTracker
                    .Entries<TEntity>()
                    .FirstOrDefault(e => e.Entity.Id == id);
                if (ee != null)
                {
                    return Delete(ee.Entity);
                }
                else
                {
                    // Чтобы не обращаться к БД создаем пустой объект с указанием только Id и отдаем его в хранилище для удаления в БД.
                    TEntity entity = new() { Id = id };
                    return Delete(entity);
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при удалении объекта {typeof(TEntity).Name} из хранилища.";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}", ex);
            }
        }

        public Result Delete<TEntity>(List<TEntity> list) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            if (DbContext == null)
            {
                string err = "EFObjectStorage не удалось получить сервис AppDbContext";
                _logger.LogError(ErrFormatString, err);
                return Result.Error(err);
            }

            foreach (TEntity entity in list)
            {
                Result res = Delete(entity);
                if(res.IsError) return res;
            }
            return Result.Success;
        }
        
        public async Task<Result> AddAndSaveAsync<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result res = Add(entity);
            if (res.IsError)
            {
                return res;
            }
            
            try
            {
                res = await SaveChangesAsync();
                return res;
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка добавления объекта {typeof(TEntity)} и сохранения изменений в БД.";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}", ex);
            }
        }

        public async Task<Result> UpdateAndSaveAsync<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            try
            {
                Result res = Update(entity);
                if (res.IsError)
                {
                    return res;
                }
                else
                {
                    var ret = await SaveChangesAsync();
                    return ret;
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка обновления объекта {typeof(TEntity)} и сохранения изменений в БД.";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}", ex);
            }
        }

        public async Task<Result> DeleteAndSaveAsync<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            try
            {
                Result res = Delete(entity);
                if (res.IsError)
                {
                    return res;
                }
                else
                {
                    var ret = await SaveChangesAsync();
                    return ret;
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка удаления объекта {typeof(TEntity)} и сохранения изменений в БД.";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}", ex);
            }
        }

        public async Task<Result> RefreshAsync<TEntity>(TEntity entity) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result res = CheckParams(entity);
            if (res.IsError) return res;
            // if (_lockDbContext.CurrentCount == 0)
            // {
            //     return Result.Error(ErrStopRecursion);
            // }
            await _lockDbContext.WaitAsync(); // Ждем освобождения ресурса
            try
            {
                await DbContext!.Entry(entity).ReloadAsync();
                return Result.Success;
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка перезагрузки из БД объекта {typeof(TEntity)}.";
                _logger.LogError(ex, ErrFormatString, strError);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}", ex);
            }
            finally
            {
                _lockDbContext.Release();
            }
        }

        public async Task<Result> SaveChangesAsync()
        {
            if (DbContext == null)
            {
                return Result.Error("EFObjectStorage неверно инициализирован DbContext");
            }

            bool isError = false;
            bool isTransactionActive = IsTransactionStarted();
            try
            {
                // 1. Очищаем промежуточные коллекции. Они будут заполняться по мере обработки объектов из очереди в контексте
                ClearSets();

                Result resValidate = await ValidateBeforeSave();
                if (resValidate.IsError) return resValidate;

                try
                {
                    await _lockDbContext.WaitAsync(); // Ждем освобождения ресурса
                    await DbContext.SaveChangesAsync();
                }
                finally
                {
                    _lockDbContext.Release();
                }

                await NotifyEventBus();

                return Result.Success;
            }
            catch (Exception ex)
            {
                isError = true;
                string strError = $"Ошибка при сохранении всех изменений в БД.";
                _logger.LogError(ex, "{StrError}: {MethodName} => {ExcError}", strError,
                    nameof(EFObjectStorage.SaveChangesAsync), ex.Message);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"{strError}: {userMessage}", ex);
            }
            finally
            {
                CallBaseEntityAfterSave(isTransactionActive, isError);                
            }
        }

        public async Task<Result> BeginTransactionAsync()
        {
            try
            {
                if (DbContext == null)
                {
                    _logger.LogError(ErrFormatString, ErrGettingAppDbContext);
                    return Result.Error(ErrGettingAppDbContext);
                }
                _transaction ??= await DbContext.Database.BeginTransactionAsync();
                // После начала транзакции в диспетчере сообщений все сообщения типа Queue будут накапливаться
                // в ожидании завершения транзакции.
                _appEventDispatcher.StartTransaction();
                return Result.Success;
            }
            catch (Exception ex)
            {
                _transaction = null;
                string err = "Ошибка при старте транзакции";
                _logger.LogError(ex, ErrFormatString, err);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"Ошибка при старте транзакции: {userMessage}", ex);
            }
        }

        public async Task<Result> CommitTransactionAsync()
        {
            bool isError = false;
            try
            {
                if (DbContext == null)
                {
                    _logger.LogError(ErrFormatString, ErrGettingAppDbContext);
                    return Result.Error(ErrGettingAppDbContext);
                }
                if (_transaction is not null)
                {
                    await _transaction.CommitAsync();
                    
                    // После успешного коммита транзакции в БД нужно отправить все накопленные оповещения в шину.
                    // Для этого нужно выполнить Commit в диспетчере сообщений
                    await _appEventDispatcher.CommitTransaction();
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                isError = true;
                string err = "Ошибка при коммите транзакции";
                _logger.LogError(ex, ErrFormatString, err);
                if (_transaction is not null)
                {
                    await _transaction.RollbackAsync();
                    _appEventDispatcher.RollbackTransaction();
                }
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"Ошибка при завершении транзакции: {userMessage}", ex);

            }
            finally
            {
                CallBaseEntityAfterSave(false, isError);
                if (_transaction is not null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }                    
            }
        }

        public async Task<Result> RollbackTransactionAsync()
        {
            try
            {
                // В случае Rollback сразу очищаем очередь оповещений, они не должны уйти
                _appEventDispatcher.RollbackTransaction();
                if (DbContext == null)
                {
                    _logger.LogError(ErrFormatString, ErrGettingAppDbContext);
                    return Result.Error(ErrGettingAppDbContext);
                }
                if (_transaction is not null)
                {
                    await _transaction.RollbackAsync();
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                string err = "Ошибка при откате транзакции";
                _logger.LogError(ex, ErrFormatString, err);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result.Error($"Ошибка при откате транзакции: {userMessage}", ex);
            }
            finally
            {
                // Вызов Rollback подразумаевает что произошла ошибка сохранения, поэтому здесь IsError = true
                CallBaseEntityAfterSave(false, true);
                if (_transaction is not null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
        }

        public bool IsTransactionStarted()
        {
            return _transaction is not null;
        }
        
        
        public async Task<Result<TEntity>> GetObjByFilterAsync<TEntity>(Expression<Func<TEntity, bool>> filter)
            where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            return await GetObjAsync<TEntity>(spec => spec.Where(filter));
        }

        public async Task<Result<TEntity>> GetObjAsync<TEntity>(int id, 
            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null,
            string strErrPrefix = "") where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            try
            {
                Result<IBaseRepository<TEntity>> resRepo = GetBaseRepository<TEntity>();
                if (resRepo.IsErrorOrNull)
                {
                    return Result<TEntity>.Error(resRepo.ErrorResult);
                }

                var ret = await resRepo.Value!.GetByIdAsync(id, querySpec);
                return ret;
            }
            catch (Exception ex)
            {
                string strErr = $"Непредвиденная ошибка при получении объекта {typeof(TEntity).Name} по его Id={id}";
                _logger.LogError(ex, ErrFormatString, strErr);
                string userMessage = DatabaseErrorHandler.GetUserFriendlyErrorMessage(ex);
                return Result<TEntity>.Error($"{strErr}: {userMessage}", ex);
            }
        }

        public async Task<Result<TEntity>> GetObjAsync<TEntity>(
            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>> querySpec)
            where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            Result<List<TEntity>> resList = await GetListAsync(querySpec);
            if (resList.IsError || resList.Value == null)
            {
                return Result<TEntity>.Error(resList.ErrorResult);
            }
            
            List<TEntity> list = resList.Value;
            if (list.Count == 0)
            {
                return Result<TEntity>.SuccessWithMessage(null, 
                    $"В БД отсутствует объект типа {typeof(TEntity).Name}, удовлетворяющий заданному условию");
            }
            if (list.Count > 1)
            {
                string err = $"В БД присутствует {list.Count} объектов типа {typeof(TEntity).Name}, " +
                             $"удовлетворяющих заданному условию, а ожидался всего один!";
                _logger.LogError("{Err}", err);
                return Result<TEntity>.Error(err);
            }
            return Result<TEntity>.Success(list[0]);
        }
        
        public bool IsDirty
        {
            get
            {
                if (_dbContext != null)
                    return _dbContext.IsDirty();
                else
                    return false;
            }
        }

        private async Task<Result> ValidateObjectAsync(BaseEntity entity)
        {
            IValidator? validator = _entityValidatorFactory.GetValidator(entity);
            if (validator != null)
            {
                var result = await validator.ValidateAsync(new ValidationContext<object>(entity));

                if (!result.IsValid)
                {
                    string strError = $"Ошибка валидации объекта {entity.Description}: {result}";
                    _logger.LogWarning("{Err}", strError);
                    return Result.Error(strError);
                }
            }
            return Result.Success;
        }


        /// <summary>
        /// Рекурсивный вызов хуков OnBefore/Save/Delete. 
        /// <para>
        /// <strong>Хуки реализованы путем регистрации обработчиков событий <see cref="IRequestHandler{TRequest}"/></strong>
        /// </para>
        /// <para>
        /// Исходим из того, что в процессе вызова хука могут создаваться/изменяться/удаляться другие объекты, 
        /// что приводит к изменению коллекции контекста ChangeTracker.Entries(). Поэтому после каждого успешеного выполнения хука объекта, 
        /// этот объект заносится в карту, сохраняющую список объектов, у которых соответствующий хук был успешно вызван 
        /// (например, после успешного выполнения OnBeforeSave объект сохраняется в коллекцию <see cref="_onBeforeSave"/>)
        /// </para>
        /// </summary>
        /// <returns></returns>
        private async Task<Result> CallHooksSyncAsync()
        {
            if (DbContext == null)
            {
                string strMsg = "Отсутствует DBContext!";
                _logger.LogError(ErrFormatString, strMsg);
                return Result.Error(strMsg);
            }

            int iObjectsProcessed;
            // Цикл нужен, поскольку внутри OnBefore/After/Save/Delete могут создаваться/изменяться/удаляться другие объекты.
            do
            {
                // Перезапрашиваем очередь изменяемых объектов из контекста, которая могла измениться после предыдущей цепочки вызовов OnBefore/After/Save/Delete
                List<EntityEntry> queue = DbContext.GetQueue(); 
                iObjectsProcessed = 0;
                foreach (EntityEntry entry in queue)
                {
                    BaseEntity? entity = entry.Entity as BaseEntity;
                    EntityStorageState state = EntityStorageState.Undefined;
                    if (entity != null)
                    {
                        // Заполняем / обновляем списки всех затронутых объектов.
                        switch(entry.State)
                        {
                            case EntityState.Added:
                                state = EntityStorageState.Added;
                                _fullListAdd.Add(entity);
                                break;
                            case EntityState.Modified:
                                state = EntityStorageState.Modified;
                                _fullListModify.Add(entity);
                                break;
                            case EntityState.Deleted:
                                state = EntityStorageState.Deleted;
                                _fullListDelete.Add(entity);
                                break;
                        }

                        if (state != EntityStorageState.Undefined)
                        {
                            Result<int> res = await CallHookAsync(entity, state);
                            if (res.IsError) return res;
                            iObjectsProcessed += res.Value;
                        }
                    }
                }
            } while (iObjectsProcessed > 0);

            return Result.Success;
        }

        private async Task<Result<int>> CallHookAsync(BaseEntity entity, EntityStorageState state)
        {
            if (_onBeforeSave.Contains(entity)) return Result<int>.Success(0);
            
            // Вызов простейших проверок на уровне объекта
            bool isDeleting = state == EntityStorageState.Deleted;
            Result res = entity.OnBeforeSave(isDeleting);
            if (res.IsError)
            {
                return Result<int>.Error(res.ErrorResult, 0);
            }
            
            var request = _appEventFactory.CreateBeforeSaveRequest(entity, this, state);
            if (request != null)
            {
                res = await _appEventDispatcher.SendAsync(request);
                if (res.IsError)
                {
                    return Result<int>.Error(res.ErrorResult, 0);
                }
            }
            // После успешного выполнения запоминаем объект в списке
            _onBeforeSave.Add(entity);
            return Result<int>.Success(1);
        }

        /// <summary>
        /// Если выполняется транзакия - оповещения будут поставлены в очередь и отправлены после успешного Commit-а
        /// Если транзакции нет - выплюнется все что есть, включая накопленное.
        /// </summary>
        private async Task NotifyEventBus()
        {
            // После успешной записи отправляем в EventBus сведения о том, что объекты сохранены/удалены
            // Если транзакция еще в процессе - просто сохраняем оповещения в списке. 
            // Важен порядок! Если сначала в транзакции прошло сохранение, а потом проведение - это должно так и остаться!
            foreach (BaseEntity entity in _fullListAdd)
            {
                var notification = _appEventFactory.CreateAfterSaveNotification(entity, this, EntityStorageState.Added);
                await _appEventDispatcher.QueueAsync(notification);
            }
            
            foreach (BaseEntity entity in _fullListModify)
            {
                var notification = _appEventFactory.CreateAfterSaveNotification(entity, this, EntityStorageState.Modified);
                await _appEventDispatcher.QueueAsync(notification);
            }
            
            foreach (BaseEntity entity in _fullListDelete)
            {
                var notification = _appEventFactory.CreateAfterSaveNotification(entity, this, EntityStorageState.Deleted);
                await _appEventDispatcher.QueueAsync(notification);
            }
        }

        private void CallBaseEntityAfterSave(bool isTransactionActive, bool isError)
        {
            // После успешной записи отправляем в EventBus сведения о том, что объекты сохранены/удалены
            // Если транзакция еще в процессе - просто сохраняем оповещения в списке. 
            // Важен порядок! Если сначала в транзакции прошло сохранение, а потом проведение - это должно так и остаться!
            foreach (BaseEntity entity in _fullListAdd)
            {
                entity.OnAfterSave(isTransactionActive, isError);
            }
            
            foreach (BaseEntity entity in _fullListModify)
            {
                entity.OnAfterSave(isTransactionActive, isError);
            }
            
            foreach (BaseEntity entity in _fullListDelete)
            {
                entity.OnAfterDelete(isTransactionActive, isError);
            }
        }
        
        private async Task<Result> ValidateBeforeSave()
        {
            Result resBefore = await CallHooksSyncAsync();
            if (resBefore.IsError) return resBefore;

            // Перед записью выполняем валидацию добавляемых объектов 
            foreach (BaseEntity entity in _fullListAdd)
            {
                Result res = await ValidateObjectAsync(entity);
                if (res.IsError) return res;
            }

            // Перед записью выполняем валидацию изменяемых объектов 
            foreach (BaseEntity entity in _fullListModify)
            {
                Result res = await ValidateObjectAsync(entity);
                if (res.IsError) return res;
            }
            return Result.Success;
        }
        
        protected AppDbContext? DbContext
        {
            get 
            {
                _dbContext ??= _services.GetService<AppDbContext>();
                return _dbContext;
            }
        }

        // Коллекции, хранящие списки объектов, у которых был успешно вызваны методы OnBefore/Save/Delete
        private readonly HashSet<object> _fullListAdd = [];
        private readonly HashSet<object> _fullListModify = [];
        private readonly HashSet<object> _fullListDelete = [];
        private readonly HashSet<object> _onBeforeSave = [];
        
        private void ClearSets()
        {
            _fullListAdd.Clear();
            _fullListModify.Clear();
            _fullListDelete.Clear();
            _onBeforeSave.Clear();
        }
    }
}
