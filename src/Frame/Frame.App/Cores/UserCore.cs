using Frame.Shared;
using Frame.App.Security;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using SmartFormat;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;
using Frame.App.IEntityRepositories;
using Frame.Domain.Params;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace Frame.App.Cores
{
    /// <summary>
    /// <para>
    /// Реализация интерфейса <see cref="IUserCore"/>.
    /// Осуществляет доступ к простейшему синглтону <see cref="IUserSecurityData"/>, 
    /// который хранит в себе карту со всеми настройками безопасности для залогинившихся пользователей.
    /// Доступ осуществляется через <see cref="IUserSecurityDataManager"/>.
    /// </para>
    /// <para>
    /// Сам <see cref="UserCore"/> создается как Scoped и существует для текущего пользователя в рамках сессии.
    /// </para>
    /// </summary>
    public class UserCore: IUserCore
    {
        private const string UserCoreNotInitializedError = "UserCore не инициализирован";
        
        private readonly IGetCurrentUserNameService _getCurrentUserNameService;
        private readonly IUserSecurityDataManager _userSecurityDataManager;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ILogger<IUserCore> _logger;
        private readonly AppCoreProvider _appCoreProvider;

        public UserCore(IGetCurrentUserNameService getCurrentUserNameService,
                        IUserSecurityDataManager userSecurityDataManager,
                        IUserProfileRepository userProfileRepository,
                        AppCoreProvider appCoreProvider,
                        ILogger<UserCore> logger) 
        {
            _getCurrentUserNameService = 
                getCurrentUserNameService ?? throw new ArgumentNullException(nameof(getCurrentUserNameService));
            _userSecurityDataManager = 
                userSecurityDataManager ?? throw new ArgumentNullException(nameof(userSecurityDataManager));
            _userProfileRepository =
                userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _appCoreProvider = 
                appCoreProvider ?? throw new ArgumentNullException(nameof(appCoreProvider));
            _logger = 
                logger ?? throw new ArgumentNullException(nameof(logger));

            CurrentUserLogin = _getCurrentUserNameService.GetLogin();
        }
        
        public async Task InitializeAsync()
        {
            if (CurrentUserLogin.Length == 0)
            {
                CurrentUserLogin = await _getCurrentUserNameService.GetLoginAsync();
            }
        }

        public string CurrentUserLogin { get; private set; }
        
        public bool IsInitialized => CurrentUserLogin.Length > 0;

        public User? CurrentUser => _userSession?.User;

        public ParamList UserParamList  => (_userSession == null) ? new ParamList() : _userSession.UserParamList;
        
        /// <summary>
        /// Сессия текущего пользователя. Каждый раз при обращении запрашивает у <see cref="IUserSecurityDataManager"/>.
        /// Если не инициализировано, или отсутствует - выбросит Exception
        /// </summary>
        /// <exception cref="Exception"></exception>
        private UserSession? _userSession
        {
            get
            {
                if (!IsInitialized)
                {
                    return null;
                }

                Result<UserSession> res = _userSecurityDataManager.GetUserSession(CurrentUserLogin);
                if (res.IsError || res.Value == null)
                {
                    string err = $"UserCore инициализирован, но сессия не загрузилась: {res.ErrorResult}";
                    _logger.LogError(err);
                    return null;
                    //throw new Exception(err);
                }
                return res.Value;
            }
        }

        private AppCore _appCore
        {
            get
            {
                if (_varAppCore == null)
                {
                    Result<AppCore> resAppCore = _appCoreProvider.GetAppCore();
                    resAppCore.CheckAndThrow("UserCore: получение AppCore");
                    _varAppCore = resAppCore.Value;
                }
                return _varAppCore!;
            }
        }
        private AppCore? _varAppCore;
        
        #region ==================== Свойства и методы по работе с пользовательским списком параметров ====================

        public async Task<Result> SaveUserProfileAsync()
        {
            try
            {
                if(!IsInitialized)
                {
                    return Result.Error(UserCoreNotInitializedError);
                }

                User? user = CurrentUser;
                if (user != null)
                {
                    user.UserProfile.SetParamList(UserParamList);
                    return await _userProfileRepository.SaveUserProfileAsync(user.UserProfile);
                }
                else
                {
                    return Result.Error("Отсутствует текущий пользователь");
                }
            }
            catch (Exception ex)
            {
                string err = "Ошибка при сохранении пользовательского списка параметров";
                _logger.LogError(ex, err);
                return Result.Error(err);
            }
        }

        #endregion

        // TODO: Future: Optimization: продумать и реализовать кэширование Security Expressions на уровне UserSession/SecurityProfile (см. подробности в файле АРХИТЕКТУРА.txt)

        #region ==================== Методы получения прав доступа ====================
        
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanAdd<TEntity>() where TEntity : BaseEntity
        {
            return EvaluateCondition<TEntity>(EntityOperationType.CreateTls);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanAdd(BaseEntity obj)
        {
            // Сначала проверяем, а можно ли выполнять действие для типа. Если нельзя - дальше для объекта вообще нет смысла анализировать.
            Result<bool> conditionForType = EvaluateCondition(obj.GetType().Name, EntityOperationType.CreateTls);
            if (conditionForType.IsError || !conditionForType.Value)
            {
                return conditionForType;
            }
            return EvaluateCondition(EntityOperationType.CreateOls, obj);
        }


        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanView<TEntity>() where TEntity : BaseEntity
        {
            return EvaluateCondition<TEntity>(EntityOperationType.ViewTls);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanView(BaseEntity obj)
        {
            // Сначала проверяем, а можно ли выполнять действие для типа. Если нельзя - дальше для объекта вообще нет смысла анализировать.
            Result<bool> conditionForType = EvaluateCondition(obj.GetType().Name, EntityOperationType.ViewTls);
            if (conditionForType.IsError || !conditionForType.Value)
            {
                return conditionForType;
            }
            return EvaluateCondition(EntityOperationType.ViewOls, obj);
        }
        
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanModify<TEntity>() where TEntity : BaseEntity
        {
            return EvaluateCondition<TEntity>(EntityOperationType.ModifyTls);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanModify(BaseEntity obj, List<ChangedPropValue>? changedPropValues = null)
        {
            // Сначала проверяем, а можно ли выполнять действие для типа. Если нельзя - дальше для объекта вообще нет смысла анализировать.
            Result<bool> conditionForType = EvaluateCondition(obj.GetType().Name, EntityOperationType.ModifyTls);
            if (conditionForType.IsError || !conditionForType.Value)
            {
                return conditionForType;
            }

            if (changedPropValues != null)
            {
                Result<bool> attrLevelCheck = _checkALS(obj, changedPropValues);
                if (attrLevelCheck.IsErrorOrNull || !attrLevelCheck.Value)
                {
                    return attrLevelCheck;
                }
            }
            return EvaluateCondition(EntityOperationType.ModifyOls, obj);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanDelete<TEntity>() where TEntity : BaseEntity
        {
            return EvaluateCondition<TEntity>(EntityOperationType.DeleteTls);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanDelete(BaseEntity obj)
        {
            // Сначала проверяем, а можно ли выполнять действие для типа. Если нельзя - дальше для объекта вообще нет смысла анализировать.
            Result<bool> conditionForType = EvaluateCondition(obj.GetType().Name, EntityOperationType.DeleteTls);
            if (conditionForType.IsError || !conditionForType.Value)
            {
                return conditionForType;
            }
            return EvaluateCondition(EntityOperationType.DeleteOls, obj);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanAudit<TEntity>() where TEntity : BaseEntity
        {
            return EvaluateCondition<TEntity>(EntityOperationType.AuditTls);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanAudit(BaseEntity obj)
        {
            // Сначала проверяем, а можно ли выполнять действие для типа. Если нельзя - дальше для объекта вообще нет смысла анализировать.
            Result<bool> conditionForType = EvaluateCondition(obj.GetType().Name, EntityOperationType.AuditTls);
            if (conditionForType.IsError || !conditionForType.Value)
            {
                return conditionForType;
            }
            return EvaluateCondition(EntityOperationType.AuditOls, obj);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanSaveAsTemplate<TEntity>() where TEntity : BaseEntity
        {
            return EvaluateCondition<TEntity>(EntityOperationType.SaveAsTemplate);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CanSaveAsTemplate(BaseEntity obj)
        {
            // Сначала проверяем, а можно ли выполнять действие для типа. Если нельзя - дальше для объекта вообще нет смысла анализировать.
            Result<bool> conditionForType = EvaluateCondition(obj.GetType().Name, EntityOperationType.SaveAsTemplate);
            if (conditionForType.IsError || !conditionForType.Value)
            {
                return conditionForType;
            }
            return EvaluateCondition(EntityOperationType.SaveAsTemplate, obj);
        }
        
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CreateFromTemplate<TEntity>() where TEntity : BaseEntity
        {
            return EvaluateCondition<TEntity>(EntityOperationType.CreateFromTemplate);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public Result<bool> CreateFromTemplate(BaseEntity obj)
        {
            // Сначала проверяем, а можно ли выполнять действие для типа. Если нельзя - дальше для объекта вообще нет смысла анализировать.
            Result<bool> conditionForType = EvaluateCondition(obj.GetType().Name, EntityOperationType.CreateFromTemplate);
            if (conditionForType.IsError || !conditionForType.Value)
            {
                return conditionForType;
            }
            return EvaluateCondition(EntityOperationType.CreateFromTemplate, obj);
        }

        
        
        public Result<IQueryable<TEntity>> ApplyReadQueryFilter<TEntity>(IQueryable<TEntity> query) where TEntity : BaseEntity
        {
            string strProcessedFilter = "";
            string entityType = "";
            
            try
            {
                Result<SecurityProfile> resProfile = _getSecurityProfile<TEntity>();
                if (resProfile.IsErrorOrNull)
                {
                    return Result<IQueryable<TEntity>>.Error(resProfile);
                }

                SecurityProfile profile = resProfile.Value!;
                if (profile.ReadQueryFilter.Length > 0)
                {
                    try
                    {
                        strProcessedFilter = Smart.Format(profile.ReadQueryFilter, _appCore);
                    }
                    catch (Exception ex)
                    {
                        string strError = $"Ошибка форматирования фильтра ReadQueryFilter {profile.ReadQueryFilter} " +
                                          $"для <{entityType}>: {ex}";
                        _logger.LogSecurity(strError, LogLevel.Error, ex);
                        return Result<IQueryable<TEntity>>.Error(strError, ex);
                    }

                    IQueryable <TEntity> queryNew = query.Where(strProcessedFilter);
                    return Result<IQueryable<TEntity>>.Success(queryNew);
                }
                
                // Если фильтр не задан - значит считаем что все разрешено.
                return Result<IQueryable<TEntity>>.Success(query);
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при наложении фильтра безопасности {strProcessedFilter} " +
                                  $"на IQuerable<{entityType}>: {ex.Message}";
                _logger.LogSecurity(strError, LogLevel.Error, ex);
                return Result<IQueryable<TEntity>>.Error(strError, ex);
            }
        }


        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public Result<List<string>> GetReadableAttrNames<TEntity>() where TEntity : BaseEntity
        {
            Result<SecurityProfile> resProfile = _getSecurityProfile<TEntity>();
            if (resProfile.IsErrorOrNull)
            {
                return Result<List<string>>.Error(resProfile);
            }

            SecurityProfile profile = resProfile.Value!;
            return Result<List<string>>.Success(profile.ReadAttrsList);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public Result<List<string>> GetEditableAttrNames<TEntity>() where TEntity : BaseEntity
        {
            Result<SecurityProfile> resProfile = _getSecurityProfile<TEntity>();
            if (resProfile.IsErrorOrNull)
            {
                return Result<List<string>>.Error(resProfile);
            }

            SecurityProfile profile = resProfile.Value!;
            return Result<List<string>>.Success(profile.ModifyAttrsList);
        }
        
        #endregion

        #region ==================== Служебные методы  ====================

        private Result<SecurityProfile> _getSecurityProfile<TEntity>() where TEntity : BaseEntity
        {
            if(!IsInitialized)
            {
                return Result<SecurityProfile>.Error(UserCoreNotInitializedError);
            }

            string entityType = typeof(TEntity).Name;
            try
            {
                UserSession? session = _userSession;
                if (session == null)
                {
                    return Result<SecurityProfile>.Error("Не удалось получить сессию пользователя");
                }
                SecurityProfile? profile = session.GetSecurityProfile(entityType);
                if (profile == null)
                {
                    // Как вариант - вернуть true, типа все разрешено.
                    string err = $"У пользователя {CurrentUserLogin} для типа {entityType} не найден профиль безопасности";
                    _logger.LogSecurity(err, LogLevel.Error);
                    return Result<SecurityProfile>.Error(err);
                }
                return Result<SecurityProfile>.Success(profile);
            }
            catch (Exception ex)
            {
                string err = $"Ошибка при получении профиля безопасности для типа {entityType}: {ex.Message}";
                _logger.LogSecurity(err, LogLevel.Error, ex);
                return Result<SecurityProfile>.Error(err);
            }
        }

        private Result<bool> EvaluateCondition(EntityOperationType operationType, BaseEntity obj)
        {
            if (CurrentUserLogin.Length == 0)
            {
                return Result<bool>.Error("Текущий пользователь не определен");
            }

            if (operationType is EntityOperationType.ModifyAttrs or EntityOperationType.ReadAttrs)
            {
                return Result<bool>.Error("Невозможно выполнить вычисление условия по списку атрибутов");
            }

            try
            {
                Result<string> resFilter = GetFilterForEntityType(obj.GetType().Name, operationType);
                if(resFilter.IsError)
                {
                    return Result<bool>.Error(resFilter.ErrorResult);
                }
                string filter = resFilter.Value ?? "";

                if (CheckForConstantTrue(filter))
                {
                    return Result<bool>.Success(true);
                }

                if (CheckForConstantFalse(filter))
                {
                    return Result<bool>.Success(false);
                }

                return EvaluateExpression(filter, obj);
            }
            catch (Exception ex)
            {
                _logger.LogSecurity(ex.ToString(), LogLevel.Error, ex);
                return Result<bool>.Error(ex.Message);
            }
        }


        private Result<bool> EvaluateCondition<TEntity>(EntityOperationType operationType) where TEntity: BaseEntity
        {
            if (CurrentUserLogin.Length == 0)
            {
                return Result<bool>.Error("Текущий пользователь не определен");
            }

            if (operationType is EntityOperationType.ModifyAttrs or EntityOperationType.ReadAttrs)
            {
                return Result<bool>.Error("Невозможно выполнить вычисление условия по списку атрибутов");
            }

            Result<string> resFilter = GetFilterForEntityType<TEntity>(operationType);
            if (resFilter.IsError)
            {
                return Result<bool>.Error(resFilter.ErrorResult);
            }
            return EvaluateExpression(resFilter.Value ?? "");
        }

        private Result<bool> EvaluateCondition(string entityTypeName, EntityOperationType operationType)
        {
            if (operationType is EntityOperationType.ModifyAttrs or EntityOperationType.ReadAttrs)
            {
                return Result<bool>.Error("Невозможно выполнить вычисление условия по списку атрибутов");
            }

            Result<string> resFilter = GetFilterForEntityType(entityTypeName, operationType);
            if (resFilter.IsError)
            {
                return Result<bool>.Error(resFilter.ErrorResult);
            }
            return EvaluateExpression(resFilter.Value ?? "");
        }

        private Result<bool> EvaluateExpression(string strExpression)
        {
            try
            {
                if (CheckForConstantTrue(strExpression))
                {
                    return Result<bool>.Success(true);
                }

                if (CheckForConstantFalse(strExpression))
                {
                    return Result<bool>.Success(false);
                }

                // AppCore appCore = new(_serverCore.SystemParamList, UserParamList, CurrentUser);
                // AppCore appCore = new(_serverCore, this);

                // Формирует динамическую функцию вида bool func(AppCore appCore)
                Func<AppCore, bool> func = BuildExpression(strExpression);

                return Result<bool>.Success(func.Invoke(_appCore));
            }
            catch (Exception ex)
            {
                _logger.LogSecurity($"Ошибка при вычислении выражения {strExpression}", LogLevel.Error, ex);
                return Result<bool>.Error(ex.ToString());
            }
        }

        private Result<bool> EvaluateExpression(string strExpression, BaseEntity obj)
        {
            try
            {
                if (CheckForConstantTrue(strExpression))
                {
                    return Result<bool>.Success(true);
                }

                if (CheckForConstantFalse(strExpression))
                {
                    return Result<bool>.Success(false);
                }

                // Формирует динамическую функцию вида bool func(TEntity obj, AppCore appCore)
                var compiledExpression = BuildExpression(obj.GetType(), strExpression);
                if (compiledExpression != null)
                {
                    object? oResult = compiledExpression.DynamicInvoke([obj, _appCore]);
                    if (oResult != null)
                    {
                        return Result<bool>.Success((bool)oResult);
                    }
                    else
                    {
                        return Result<bool>.Error("Ошибка! Проверка условия вернула результат null");
                    }
                }
                else
                {
                    return Result<bool>.Error("Ошибка получения выражения");
                }
            }
            catch (Exception ex)
            {
                _logger.LogSecurity($"Ошибка при вычислении выражения {strExpression}", LogLevel.Error, ex);
                return Result<bool>.Error(ex.ToString());
            }
        }

        private Result<bool> _checkALS(BaseEntity obj, List<ChangedPropValue> changedPropValues)
        {
            string typeName = obj.GetType().Name;
            Result<bool> res = _checkALS(typeName, EntityOperationType.ModifyAttrs, changedPropValues);
            if(res.IsErrorOrNull || !res.Value) return res;
            
            return _checkALS(typeName, EntityOperationType.ReadAttrs, changedPropValues);
        }

        private Result<bool> _checkALS(string typeName, EntityOperationType attrListType, List<ChangedPropValue> changedPropValues)
        {
            Result<List<string>> resProps = GetPropList(typeName, attrListType);
            if(resProps.IsErrorOrNull) return Result<bool>.Error(resProps.ErrorResult);
            List<string> allowedProps = resProps.Value!;
            
            // Если ограничений по свойствам нет - возвращаем true
            if (allowedProps.Count == 0) return Result<bool>.Success(true);

            // Если хотя бы одно из изменяемых свойств не содержится в списке разрешенных - возвращаем false
            foreach (ChangedPropValue prop in changedPropValues)
            {
                if (!allowedProps.Contains(prop.AttrName))
                {
                    return Result<bool>.SuccessWithMessage(false, $"Изменение свойства {prop.AttrName} запрещено!");
                }
            }
            return Result<bool>.Success(true);
        }
        
        /// <summary>
        /// Формирует выражение в виде строго типизированной функции <b>bool Function(AppCore appCore)</b> телом которй является condition.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static Func<AppCore, bool> BuildExpression(string condition)
        {
            ParameterExpression appCoreParam = Expression.Parameter(typeof(AppCore), "appCore");

            LambdaExpression expression = DynamicExpressionParser.ParseLambda(
                [appCoreParam],
                typeof(bool),
                condition);

            return (Func<AppCore, bool>) expression.Compile();
        }

        /// <summary>
        /// Формирует выражение в виде делегата слаботипизированной функции в формате <b>bool Func(ObjectType obj, AppCore appCore)</b> телом которой является condition.
        /// Данная функция должна вызываться с использованием DynamicInvoke.
        /// </summary>
        /// <param name="objectType">Тип передаваемого объекта</param>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static Delegate BuildExpression(Type objectType, string condition)
        {
            ParameterExpression entityParam = Expression.Parameter(objectType, "obj");
            ParameterExpression appCoreParam = Expression.Parameter(typeof(AppCore), "appCore");

            LambdaExpression expression = DynamicExpressionParser.ParseLambda(
                [entityParam, appCoreParam],
                typeof(bool),
                condition);

            return expression.Compile();
        }

        // /// <summary>
        // /// Формирует выражение в виде строго типизированной функции в формате <b>bool Func(TEntity obj, AppCore appCore)</b> телом которой является condition.
        // /// </summary>
        // /// <typeparam name="TEntity"></typeparam>
        // /// <param name="condition"></param>
        // /// <returns></returns>
        // private static Func<TEntity, AppCore, bool> BuildExpression<TEntity>(string condition)
        // {
        //     ParameterExpression entityParam = Expression.Parameter(typeof(TEntity), "obj");
        //     ParameterExpression appCoreParam = Expression.Parameter(typeof(AppCore), "appCore");
        //
        //     LambdaExpression expression = DynamicExpressionParser.ParseLambda(
        //         [entityParam, appCoreParam],
        //         typeof(bool),
        //         condition);
        //
        //     return (Func<TEntity, AppCore, bool>) expression.Compile();
        // }

        private static bool CheckForConstantTrue(string filter)
        {
            string filterUCase = filter.ToUpper();
            if ((filterUCase.Length == 0) || filterUCase == "TRUE" || filterUCase == "1" || filterUCase == "YES" || filterUCase == "OK")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool CheckForConstantFalse(string filter)
        {
            string filterUCase = filter.ToUpper();
            if (filterUCase is "FALSE" or "0" or "NO" or "CANCEL")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private Result<List<string>> GetPropList(string entityType, EntityOperationType operationType)
        {
            if (operationType is not EntityOperationType.ModifyAttrs and not EntityOperationType.ReadAttrs)
            {
                string err = $"Некорректный вызов: для операции {Enum.GetName(operationType)} нужно вызывать метод {nameof(GetFilterForEntityType)}";
                _logger.LogSecurity(err, LogLevel.Error);
                return Result<List<string>>.Error(err);
            }

            try
            {
                UserSession? session = _userSession;
                if (session == null)
                {
                    // Если по какой-то причине не удалось получить сессию пользователя - считаем что нет доступа
                    string err = $"Не удалось получить сессию пользователя для получения фильтра по типу {entityType} по операции {Enum.GetName(operationType)}";
                    _logger.LogSecurity(err, LogLevel.Error);
                    return Result<List<string>>.Error(err);
                }
                SecurityProfile? profile = session.GetSecurityProfile(entityType);
                if (profile == null)
                {
                    // Если профиль безопасности для данного типа не задан - ограничений нет.
                    return Result<List<string>>.Success([]);
                }
                if(operationType == EntityOperationType.ModifyAttrs) return Result<List<string>>.Success(profile.ModifyAttrsList);
                if(operationType == EntityOperationType.ReadAttrs) return Result<List<string>>.Success(profile.ReadAttrsList);

                return Result<List<string>>.Success([]);
            }
            catch (Exception ex)
            {
                string err = $"Ошибка при получении списка свойств для {entityType} по операции {Enum.GetName(operationType)}: {ex.Message}";
                _logger.LogSecurity(err, LogLevel.Error, ex);
                return Result<List<string>>.Error(err, ex);
            }
            
        }
                
        private Result<string> GetFilterForEntityType(string entityType, EntityOperationType operationType)
        {
            if (operationType is EntityOperationType.ModifyAttrs or EntityOperationType.ReadAttrs)
            {
                string err = $"Некорректный вызов: для операции {Enum.GetName(operationType)} нужно вызывать метод {nameof(GetPropList)}";
                _logger.LogSecurity(err, LogLevel.Error);
                return Result<string>.Error(err);
            }
            try
            {
                UserSession? session = _userSession;
                if (session == null)
                {
                    // Если по какой-то причине не удалось получить сессию пользователя - считаем что нет доступа
                    string err = $"Не удалось получить сессию пользователя для получения фильтра по типу {entityType} по операции {Enum.GetName(operationType)}";
                    _logger.LogSecurity(err, LogLevel.Error);
                    return Result<string>.Error(err);
                }
                SecurityProfile? profile = session.GetSecurityProfile(entityType);
                if (profile == null)
                {
                    // Если профиль безопасности для данного типа не задан - ограничений нет.
                    return Result<string>.Success("true");
                }
                string strFilter = profile.GetFilter(operationType);

                return Result<string>.Success(Smart.Format(strFilter, _appCore));
            }
            catch (Exception ex)
            {
                string err = $"Ошибка при получении фильтра для {entityType} по операции {Enum.GetName(operationType)}: {ex.Message}";
                _logger.LogSecurity(err, LogLevel.Error, ex);
                return Result<string>.Error(err, ex);
            }
        }

        private Result<string> GetFilterForEntityType<TEntity>(EntityOperationType operationType) where TEntity : BaseEntity
        {
            return GetFilterForEntityType(typeof(TEntity).Name, operationType);
        }

        #endregion
        
    }
}
