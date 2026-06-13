using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Frame.Shared;
using Frame.Domain.Entities.Core;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Frame.App.IEntityRepositories;
using Frame.Domain.QuerySpec;

namespace Frame.Infrastructure.EntityRepositories
{
    public enum EFBaseRepositoryParams
    {
        ObjectStorage,
        Validator,
        ConfigureQuerySpecification,
        PreloadDataIntoObjectStorage,
        
        [Obsolete("Данное значение устарело! Нужно использовать QuerySpecification.")]
        ConfigureSelect,
        [Obsolete("Данное значение устарело! Нужно использовать QuerySpecification.")]
        ConfigureWhere,
        [Obsolete("Данное значение устарело! Нужно использовать QuerySpecification.")]
        ConfigureOrderBy
    }
    public class EFBaseRepositoryOptions<TEntity> where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
    {
        public Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? ConfigureQuerySpecification { get; set; }
        public Action<IObjectStorage>? PreloadDataIntoObjectStorage { get; set; }
        
        [Obsolete("Данное свойство устарело! Нужно использовать QuerySpecification.")]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureSelect { get; set; }
        [Obsolete("Данное свойство устарело! Нужно использовать QuerySpecification.")]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureWhere { get; set; }
        [Obsolete("Данное свойство устарело! Нужно использовать QuerySpecification.")]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureOrderBy { get; set; }
    }

    public static class EFBaseRepositoryExtensions
    {
        public static IServiceCollection AddBaseRepository<TEntity>(this IServiceCollection services, Action<EFBaseRepositoryOptions<TEntity>> setupAction) where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
        {
            services.Configure(setupAction);
            services.AddTransient<IBaseRepository<TEntity>, EFBaseRepository<TEntity>>();
            return services;
        }
    }
    /// <summary>
    /// Базовый репозиторий может работать с любым DbContext, который реализует интерфейс IAppDbContext.
    /// Это может быть как AppDbContext так и NoSecurityDbContext
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EFBaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
    {
        protected IObjectStorage _objectStorage;
        protected readonly ILogger<EFBaseRepository<TEntity>> _logger;
        protected IValidator<TEntity>? _validator;

        /// <summary>
        /// Делегат, который будет вызван при конструировании запроса для предварительной загрузки в контекст связанных данных
        /// </summary>
        public Action<IObjectStorage>? PreloadDataIntoObjectStorage { get; set; }

        public Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? ConfigureQuerySpecification { get; set; }

        /// <summary>
        /// Делегат, который будет вызван при конструировании запроса для формирования секции Select
        /// </summary>
        [Obsolete("Делегат устарел! Нужно использовать ConfigureQuerySpecification.")]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureSelect { get; set; }
        /// <summary>
        /// Делегат, который будет вызван при конструировании запроса для формирования секции Where
        /// </summary>
        [Obsolete("Делегат устарел! Нужно использовать ConfigureQuerySpecification.")]
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureWhere { get; set; }
        /// <summary>
        /// Делегат, который будет вызван при конструировании запроса для формирования секции OrderBy
        /// </summary>
        [Obsolete("Делегат устарел! Нужно использовать ConfigureQuerySpecification.")]
        
        public Func<IQueryable<TEntity>, IQueryable<TEntity>>? ConfigureOrderBy { get; set; }

        private const string ErrFormatString = "{StrError}";
        public EFBaseRepository(IObjectStorage objectStorage, 
                                ILogger<EFBaseRepository<TEntity>>? logger, 
                                IValidator<TEntity>? validator = null, 
                                IOptions<EFBaseRepositoryOptions<TEntity>>? options = null)
        {
            _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator;
            EFBaseRepositoryOptions<TEntity>? opt = options?.Value ?? null;

            if (opt != null)
            {
                PreloadDataIntoObjectStorage = opt.PreloadDataIntoObjectStorage;
                ConfigureQuerySpecification = opt.ConfigureQuerySpecification;
                
                ConfigureSelect = opt.ConfigureSelect;
                ConfigureWhere = opt.ConfigureWhere;
                ConfigureOrderBy = opt.ConfigureOrderBy;
            }
        }

        public Result Configure(Dictionary<int, object> args)
        {
            if (args is { Count: 0 })
            {
                return Result.Success;
            }
            
            if (args.TryGetValue((int)EFBaseRepositoryParams.ObjectStorage, out object? objectStorageTmp))
            {
                if (objectStorageTmp is IObjectStorage tmp1)
                {
                    _objectStorage = tmp1;
                }
                else
                {
                    string strError = "Переданный в метод Configure параметр DbContext не является объектом типа IObjectStorage";
                    _logger.LogError(strError);
                    return Result.Error(strError);
                }
            }
            if (args.TryGetValue((int)EFBaseRepositoryParams.Validator, out object? validatorTmp) 
                && validatorTmp is IValidator<TEntity> tmp3)
            {
                _validator = tmp3;
            }
            if (args.TryGetValue((int)EFBaseRepositoryParams.PreloadDataIntoObjectStorage, out object? preLoadTmp) 
                && preLoadTmp is Action<IObjectStorage> tmpPreLoad)
            {
                PreloadDataIntoObjectStorage = tmpPreLoad;
            }
            if (args.TryGetValue((int)EFBaseRepositoryParams.ConfigureQuerySpecification, out object? specTmp) 
                && specTmp is Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>> tmp4)
            {
                ConfigureQuerySpecification = tmp4;
            }
            
            if (args.TryGetValue((int)EFBaseRepositoryParams.ConfigureSelect, out object? selectTmp) 
                && selectTmp is Func<IQueryable<TEntity>, IQueryable<TEntity>> tmp5)
            {
                ConfigureSelect = tmp5;
            }
            if (args.TryGetValue((int)EFBaseRepositoryParams.ConfigureWhere, out object? whereTmp) 
                && whereTmp is Func<IQueryable<TEntity>, IQueryable<TEntity>> tmp6)
            {
                ConfigureSelect = tmp6;
            }
            if (args.TryGetValue((int)EFBaseRepositoryParams.ConfigureOrderBy, out object? orderByTmp) 
                && orderByTmp is Func<IQueryable<TEntity>, IQueryable<TEntity>> tmp7)
            {
                ConfigureOrderBy = tmp7;
            }

            return Result.Success;
        }


        public virtual IObjectStorage ObjectStorage => _objectStorage;

        public virtual async Task<Result<List<TEntity>>> GetAllAsync()
        {
            try
            {
                return await GetListAsync();
            }
            catch (Exception ex)
            {
                string strError = $"Не удалось получить список {typeof(TEntity).Name}: {ex.Message}";
                _logger.LogError(ex, ErrFormatString, strError);
                return Result<List<TEntity>>.Error(strError, null, ex);
            }
        }

        public virtual async Task<Result<List<TEntity>>> GetAllAsync(Expression<Func<TEntity, bool>> filter)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(filter);

                IQuerySpecification<TEntity> querySpecification = new QuerySpecification<TEntity>();
                
                // Если установлен общий конфигуратор - сначала используем его, там могут быть Include, OrderBy
                if (ConfigureQuerySpecification != null)
                {
                    querySpecification = ConfigureQuerySpecification.Invoke(querySpecification);
                }

                // Теперь устанавливаем новый фильтр
                querySpecification.Where(filter);
                
                return await GetListAsync(spec => spec.Where(filter));
            }
            catch (Exception ex)
            {
                string strError = $"Не удалось получить список {typeof(TEntity).Name}: {ex.Message}";
                _logger.LogError(ex, ErrFormatString, strError);
                return Result<List<TEntity>>.Error(strError, null, ex);
            }
        }

        public virtual async Task<Result<TEntity>> GetByIdAsync(int id, Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null)
        {
            try
            {
                Result<List<TEntity>> resList;
                
                if (querySpec != null)
                {
                    resList = await GetListAsync(x => 
                        querySpec(x).Where(c => c.Id == id));
                }
                else if (ConfigureQuerySpecification != null)
                {
                    resList = await GetListAsync(x => 
                        ConfigureQuerySpecification(x).Where(c => c.Id == id));
                }
                else
                {
                    resList = await GetListAsync(x => 
                        x.Where(c => c.Id == id));
                }
                
                if(resList.IsError || resList.Value == null)
                {
                    return Result<TEntity>.Error(resList.ErrorResult);
                }
                
                List<TEntity> list = resList.Value;
                if (list.Count == 0)
                {
                    return Result<TEntity>.SuccessWithMessage(null, $"{typeof(TEntity).Name} c id={id} не найдено в БД");
                }
                
                if(list.Count > 1)
                {
                    string err = $"В БД найдено несколько ({list.Count}) объектов {typeof(TEntity).Name} c id={id}.";
                    _logger.LogError(ErrFormatString, err);
                    return Result<TEntity>.Error(err);
                }
                
                return Result<TEntity>.Success(list[0]);
                
                // // Если спецификация передана в аргументе - берем ее, если нет - берем ту которая установлена на весь репозиторий
                // if (querySpec != null)
                // {
                //     spec = new QuerySpecification<TEntity>();
                //     spec = querySpec.Invoke(spec);
                // }
                // else if (ConfigureQuerySpecification != null)
                // {
                //     spec = ConfigureQuerySpecification.Invoke(new QuerySpecification<TEntity>());
                // }
                // else
                // {
                //     spec = new QuerySpecification<TEntity>();
                // }
                //
                // // Прописываем фильтр по Id
                // spec.Where(c => c.Id == id);
                //
                // Result<List<TEntity>> resQuery = await GetListAsync(spec);
                // if(resQuery.IsError || resQuery.Value == null)
                // {
                //     return Result<TEntity>.Error(resQuery.ErrorResult);
                // }
                //
                // List<TEntity> list = resQuery.Value;
                // if (list.Count == 0)
                // {
                //     return Result<TEntity>.SuccessWithMessage(null, $"{typeof(TEntity).Name} c id={id} не найдено в БД");
                // }
                //
                // if(list.Count > 1)
                // {
                //     return Result<TEntity>.Error($"В БД найдено несколько ({list.Count}) объектов {typeof(TEntity).Name} c id={id}.");
                // }
                //
                // return Result<TEntity>.Success(list[0]);
            }
            catch (Exception ex)
            {
                string strError = $"{typeof(TEntity).Name} c id={id}: ошибка чтения из БД: {ex.Message}";
                _logger.LogError(ex, ErrFormatString, strError);
                return Result<TEntity>.Error(strError, null, ex);
            }
        }
    
        public async Task<Result<List<TEntity>>> GetListAsync(Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? querySpec = null)
        {
            ArgumentNullException.ThrowIfNull(_objectStorage);
            PreloadDataIntoObjectStorage?.Invoke(_objectStorage);

            // TODO: Удалить старую ветку настроек после того как все зависимости будут исправлены 
            if (ConfigureSelect != null || ConfigureWhere != null || ConfigureOrderBy != null)
            {
                if (querySpec != null)
                {
                    string err = $"Выполняется метод {nameof(GetListAsync)} для типа {typeof(TEntity).Name} " +
                                 $"с передачей {nameof(IQuerySpecification<TEntity>)}, " +
                                 $"при этом {nameof(EFBaseRepository<TEntity>)} сконфигурирован по-старому! " +
                                 $"Переданный {nameof(IQuerySpecification<TEntity>)} проигнорирован!!!";
                    _logger.LogWarning("{Err}", err);
                }
                
                Result<IQueryable<TEntity>> resQuery = _objectStorage.GetQuery<TEntity>();
                if(resQuery.IsError || resQuery.Value == null)
                {
                    return Result<List<TEntity>>.Error(resQuery.ErrorResult);
                }
                else
                {
                    IQueryable<TEntity> query = resQuery.Value;
                    query = ConfigureSelect != null ? ConfigureSelect.Invoke(query) : query;
                    query = ConfigureWhere != null ? ConfigureWhere.Invoke(query) : query;
                    query = ConfigureOrderBy != null ? ConfigureOrderBy.Invoke(query) : query;

                    List<TEntity> list = await query.ToListAsync();
                    return Result<List<TEntity>>.Success(list);       // AsNoTracking().
                }
            }

            Func<IQuerySpecification<TEntity>, IQuerySpecification<TEntity>>? spec = null;

            // Если спецификация передана в аргументе - берем ее, если нет - берем ту которая установлена на весь репозиторий
            // Если на весь репозиторий ничего не установлено - будет вызван метод с аргументом null, и он вернет полный список
            if (querySpec != null)
            {
                spec = querySpec;
            }
            else if (ConfigureQuerySpecification != null)
            {
                spec = ConfigureQuerySpecification;
            } 
            
            return await _objectStorage.GetListAsync<TEntity>(spec);
        }
        
        #region ========== Синхронные методы Add/Update/Delete ==========

        public virtual Result Add(TEntity entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity);
                ArgumentNullException.ThrowIfNull(_objectStorage);
                Result<TEntity> result = ValidateObject(entity);
                if (result.IsError)
                {
                    return result;
                }
                else
                {
                    return _objectStorage.Add(entity);
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка добавления {typeof(TEntity).Name}: {ex.Message}";
                _logger.LogError(ex, ErrFormatString, strError);
                return Result.Error(strError, ex);
            }
        }

        public virtual Result Update(TEntity entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity);
                ArgumentNullException.ThrowIfNull(_objectStorage);
                if (entity.Id == 0)
                {
                    return Result<TEntity>.Error("Нельзя пытаться обновить в репозитории объект, у которого не указан Id");
                }

                Result<TEntity> result = ValidateObject(entity);
                if (result.IsError)
                {
                    return result;
                }
                else
                {
                    return _objectStorage.Update(entity);
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка обновления {typeof(TEntity).Name} c id={entity.Id}: {ex.Message}";
                _logger.LogError(ex, ErrFormatString, strError);
                return Result.Error(strError, ex);
            }
        }

        public virtual Result Delete(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            try
            {
                ArgumentNullException.ThrowIfNull(_objectStorage);
                return _objectStorage.Delete(entity);
            }
            catch (Exception ex)
            {
                string strMessage = $"Ошибка удаления {typeof(TEntity).Name} с Id = {entity.Id}: {ex.Message}";
                _logger.LogError(ex, ErrFormatString, strMessage);
                return Result.Error(strMessage, ex);
            }
        }

        public virtual Result Delete(int id)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_objectStorage);
                return _objectStorage.Delete<TEntity>(id);
            }
            catch (Exception ex)
            {
                string strMessage = $"Ошибка удаления {typeof(TEntity).Name} с Id = {id}: {ex.Message}";
                _logger.LogError(ex, ErrFormatString, strMessage);
                return Result.Error(strMessage, ex);
            }
        }

        #endregion

        #region ========== Асинхронные методы Add/Update/Delete ==========
        public virtual Task<Result> AddAsync(TEntity entity)
        {
            Result res = Add(entity);
            return Task.FromResult(res);
        }

        public virtual Task<Result> UpdateAsync(TEntity entity)
        {
            Result res = Update(entity);
            return Task.FromResult(res);
        }

        public virtual Task<Result> DeleteAsync(int id)
        {
            Result res = Delete(id);
            return Task.FromResult(res);
        }

        public virtual Task<Result> DeleteAsync(TEntity entity)
        {
            Result res = Delete(entity);
            return Task.FromResult(res);
        }

        #endregion

        public virtual Result<IQueryable<TEntity>> GetQuerable()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_objectStorage);
                Result<IQueryable<TEntity>> resQry = _objectStorage.GetQuery<TEntity>();
                if (resQry.Value == null)
                {
                    return Result<IQueryable<TEntity>>.Error($"Ошибка получения списка объекта типа {typeof(TEntity)}: вернулся null");
                }
                else
                {
                    return resQry;
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка получения списка объекта типа {typeof(TEntity)}: {ex.Message}";
                _logger.LogError(ex, "{StrError}, {Ex}", strError, ex.StackTrace);
                return Result<IQueryable<TEntity>>.Error(strError, ex);
            }
        }

        public virtual async Task<Result> RefreshAsync(TEntity entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_objectStorage);
                ArgumentNullException.ThrowIfNull(entity);
                await _objectStorage.RefreshAsync(entity);
                return Result.Success;
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка перезагрузки из БД объекта {typeof(TEntity)}: {ex.Message}";
                _logger.LogError(ex, "{StrError}, {Ex}", strError, ex.StackTrace);
                return Result<IQueryable<TEntity>>.Error(strError, ex);
            }
        }

        /// <summary>
        /// Валидация объекта перед записью с использованием зарегистрированного или переданного репозиторию валидатора для данного типа.
        /// <para>
        /// Валидатор осуществляет окончательную донастройку/проверку объекта и всех вложенных подъобъектов (связей).
        /// Если разработчик не предусмотрел каскадную проверку связанных объектов - автоматическое формирование и вызов валидаторов связанных объектов не осуществляется.
        /// </para>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual Result<TEntity> ValidateObject(TEntity data)
        {
            if (_validator != null)
            {
                FluentValidation.Results.ValidationResult vResult = _validator.Validate(data);
                if (!vResult.IsValid)
                {
                    StringBuilder sb = new();
                    foreach (string failure in vResult.Errors.Select(failure => failure.ErrorMessage))
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(" === ");
                        }
                        sb.Append(failure);
                    }
                    return Result<TEntity>.Error(sb.ToString(), data);
                }
            }

            return Result<TEntity>.Success(data);
        }
    }
}
