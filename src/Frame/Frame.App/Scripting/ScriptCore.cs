using Frame.Domain.Entities.Core;
using Frame.App.IEntityRepositories;
using Frame.Shared;
using Microsoft.Extensions.Logging;
using Westwind.Scripting;
using Frame.App.Cores;
using Frame.Domain.Entities.Core.Scripting;
using Frame.App.IEntityLoaders;
using Frame.Domain.Params;
using System.Collections.Concurrent;
using Frame.App.EventBus.Handlers.Notify;
using Frame.App.Security;
using Frame.Domain.Entities.Core.Security;

namespace Frame.App.Scripting
{
    public class ScriptCore(
        IObjectStorageProvider objectStorageProvider,
        AppCoreProvider appCoreProvider,
        IFrameSettingsLoader frameSettingsLoader,
        IAuditService auditService,
        ILogger<ScriptCore> logger) : IScriptCore
    {
        private readonly IObjectStorageProvider _objectStorageProvider =
            objectStorageProvider ?? throw new ArgumentNullException(nameof(objectStorageProvider));
        private readonly AppCoreProvider _appCoreProvider =
            appCoreProvider ?? throw new ArgumentNullException(nameof(appCoreProvider));
        private readonly IFrameSettingsLoader _frameSettingsLoader =
            frameSettingsLoader ?? throw new ArgumentNullException(nameof(frameSettingsLoader));
        private readonly IAuditService _auditService = 
            auditService ?? throw new ArgumentNullException(nameof(auditService));
        private readonly ILogger<ScriptCore> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        private FrameSettings? _frameSettings;

        private const string _errFmt = "{Err}";

        
        #region ========== Статическая карта с защищенным многопоточным доступом ========
        private static readonly object _lockMaps = new();

        private static bool _isInitialized;
        /// <summary>
        /// Карта имя класса скрипта для <see cref="EntityScript"/> - исходный код и параметры скрипта. Инициализируется при первой загрузке ScriptCore из БД.
        /// При пересохранении любого экзмпляра <see cref="EntityScript"/> данная карта должна очищаться, чтобы быть повторно перезачитана с новыми скриптами.
        /// </summary>
        private static readonly Dictionary<string, EntityScriptData> mapEntityScriptData = [];

        /// <summary>
        /// Карта для сохранения скомпилированных классов из функций. 
        /// При пересохранении любого экзмпляра данная карта должна очищаться, чтобы быть повторно перезачитана с новыми функциями.
        /// </summary>
        private static readonly ConcurrentDictionary<string, IScriptFunction> mapScriptFunction = [];

        /// <summary>
        /// Карта [<see cref="ScriptCommand"/>.Id - исходный код и параметры скрипта]. Постепенно наполняется по мере обращения к объектам <see cref="ScriptCommand"/>.
        /// При пересохранении любого экзмпляра <see cref="ScriptCommand"/> соответствующая запись в карте удаляется (см. <see cref="ScriptCommandAfterSaveNotificationHandler"/>).
        /// </summary>
        private static readonly Dictionary<int, ScriptCommandData> mapScriptCommandData = [];
        #endregion

        private static readonly object _lockExecutor = new(); 

        public async Task<Result> InitializeAsync(IEntityScriptLoader entityScriptLoader)
        {
            if (_isInitialized)
            {
                return Result.SuccessWithMessage("Уже инициализировано. Если инициализация неоходима - сначала нужно вызвать Reset.");
            }
            return await InitAsync(entityScriptLoader);
        }

        private async Task<AppCore?> _getAppCoreAsync()
        {
            if (_varAppCore == null)
            {
                Result<AppCore> resAppCore = await _appCoreProvider.GetAppCoreAsync();
                _varAppCore = resAppCore.Value;
            }
            return _varAppCore;
        }
        private AppCore? _varAppCore;

        private IObjectStorage? _objectStorage
        {
            get
            {
                Result<IObjectStorage> r = _objectStorageProvider.GetObjectStorage();
                if(r is { IsError: false, Value: not null })
                {
                    return r.Value;
                }
                else
                {
                    return null;
                }
            }
        }

        public void Reset()
        {
            lock (_lockMaps)
            {
                mapEntityScriptData.Clear();
                mapScriptCommandData.Clear();
                _frameSettings = null;
                _isInitialized = false;
            }
        }

        private async Task InitFrameSettingsAsync()
        {
            if (_frameSettings is null)
            {
                await ReloadFrameSettingsAsync();
            }
        }

        public async Task<Result> ReloadFrameSettingsAsync()
        {
            Result<FrameSettings> resSettings = await _frameSettingsLoader.InitFrameSettingsAsync();
            if (resSettings.IsErrorOrNull)
            {
                return resSettings;
            }
            _frameSettings = resSettings.Value;
            return Result.Success;
        }

        public Result ReloadEntityScript<TEntity>(EntityScript script) where TEntity : BaseEntity, new()
        {
            if (script == null)
            {
                string strError = $"В {nameof(ReloadEntityScript)} передан нулевой экземпляр объекта";
                _logger.LogError(_errFmt, strError);
                return Result.Error(strError);
            }

            if (!_isInitialized)
            {
                string strError = $"{nameof(ReloadEntityScript)}({script.Description}): ScriptCore не инициализирован!";
                _logger.LogError(_errFmt, strError);
                return Result.Error(strError);
            }

            lock (_lockMaps)
            {
                try
                {
                    Result<int> res = SaveToMap(script);
                    if(res.IsError)
                    {
                        return res;
                    }
                    else
                    {
                        // Компиляцию здесь выполняем только для того, чтобы сразу же сообщить об ошибках, если таковые будут
                        return EntityScript_CompileAndSaveToMap(EntityScript.Meta.BuildScriptClassName(script));
                    }
                }
                catch (Exception ex)
                {
                    string strError = $"Ошибка перезагрузки метаданных для {script.Description}";
                    _logger.LogError(ex, _errFmt, strError);
                    return Result.Error(strError, ex);
                }
            }
        }

        public Result RemoveEntityScript(EntityScript script)
        {
            if (script == null)
            {
                string strError = $"В {nameof(RemoveEntityScript)} передан нулевой экземпляр объекта";
                _logger.LogError(_errFmt, strError);
                return Result.Error(strError);
            }

            if (!_isInitialized)
            {
                string strError = $"{nameof(RemoveEntityScript)}({script.Description}): ScriptCore не инициализирован!";
                _logger.LogError(_errFmt, strError);
                return Result.Error(strError);
            }

            string className = EntityScript.Meta.BuildScriptClassName(script);
            lock (_lockMaps)
            {
                try
                {
                    mapEntityScriptData.Remove(className);
                    return Result.Success;
                }
                catch (Exception ex)
                {
                    string strError = $"Ошибка удаления из кэша {script.Description}";
                    _logger.LogError(ex, _errFmt, strError);
                    return Result.Error(strError, ex);
                }
            }
        }

        public async Task<Result> ReloadScriptCommandAsync(int scriptCommandId)
        {
            Result result = RemoveScriptCommand(scriptCommandId);
            if (result.IsError)
            {
                return result;
            }

            return await CompileScriptCommandAsync(scriptCommandId);
        }

        public Result RemoveScriptCommand(int scriptCommandId)
        {
            if (scriptCommandId == 0)
            {
                string strError = $"В {nameof(RemoveScriptCommand)} передан нулевой Id";
                _logger.LogError(_errFmt, strError);
                return Result.Error(strError);
            }

            if (!_isInitialized)
            {
                string strError = $"{nameof(RemoveScriptCommand)}(id = {scriptCommandId}): ScriptCore не инициализирован!";
                _logger.LogError(_errFmt, strError);
                return Result.Error(strError);
            }

            lock (_lockMaps)
            {
                try
                {
                    mapScriptCommandData.Remove(scriptCommandId);
                    return Result.Success;
                }
                catch (Exception ex)
                {
                    string strError = $"Ошибка удаления из кэша ScriptCommand (Id = {scriptCommandId})";
                    _logger.LogError(ex, _errFmt, strError);
                    return Result.Error(strError, ex);
                }
            }
        }

        public async Task<Result> ExecuteEntityScriptAsync(BaseEntity entity, EHookType hookType, EThreadType threadType)
        {
            if (entity == null)
            {
                string strError = $"В ExecuteEntityScript передан нулевой экземпляр объекта";
                _logger.LogError(_errFmt, strError);
                return Result.Error(strError);
            }

            if (!_isInitialized)
            {
                string strError = $"ExecuteEntityScript({entity.Description}): ScriptCore не инициализирован!";
                _logger.LogError(_errFmt, strError);
                return Result.Error(strError);
            }

            Result<IEntityScriptMethod> resScriptMeth = GetEntityScriptObject(entity, hookType);
            if (resScriptMeth.IsError)
            {
                return Result.Error(resScriptMeth.ErrorResult);
            }
            else if (resScriptMeth.Value == null)
            {
                // Нет скрипта, нечего выполнять.
                return Result.Success;
            }
            else
            {
                switch (threadType)
                {
                    case EThreadType.Sync:
                        IEntityScriptMethodSync? entityScriptMethodSync = resScriptMeth.Value as IEntityScriptMethodSync;
                        if (entityScriptMethodSync == null)
                        {
                            string className = EntityScript.Meta.BuildScriptClassName(entity, hookType);
                            string strError = $"{className}, объявленный как синхронный, не реализует интерфейс {nameof(IEntityScriptMethodSync)}!";
                            _logger.LogError(_errFmt, strError);
                            return Result.Error(strError);
                        }
                        else
                        {
                            AppCore? appCore = await _getAppCoreAsync();
                            if (appCore == null)
                            {
                                string err = $"Отказ в синхронном выполнении скриптового расширения {entity}=>{hookType}: " +
                                             "не удалось получить экземпляр AppCore";
                                _logger.LogError(_errFmt, err);
                                return Result.Error(err);
                            }

                            return entityScriptMethodSync.Execute(appCore, entity);
                        }
                    case EThreadType.Async:
                        IEntityScriptMethodAsync? entityScriptMethodAsync = resScriptMeth.Value as IEntityScriptMethodAsync;
                        if (entityScriptMethodAsync == null)
                        {
                            string className = EntityScript.Meta.BuildScriptClassName(entity, hookType);
                            string strError = $"{className}, объявленный как асинхронный, не реализует интерфейс {nameof(IEntityScriptMethodAsync)}!";
                            _logger.LogError(_errFmt, strError);
                            return Result.Error(strError);
                        }
                        else
                        {
                            AppCore? appCore = await _getAppCoreAsync();
                            if (appCore == null)
                            {
                                string err = $"Отказ в асинхронном выполнении скриптового расширения {entity}=>{hookType}: " +
                                             $"не удалось получить экземпляр AppCore";
                                _logger.LogError(_errFmt, err);
                                return Result.Error(err);
                            }

                            return await entityScriptMethodAsync.ExecuteAsync(appCore, entity);
                        }
                    default:
                        return Result.Error($"Ошибка, которая никогда не должна произойти: передан неизвестный тип thread: {threadType}");
                }
            }
        }

        public async Task<Result<dynamic>> ExecuteScriptCommandAsync(int scriptCommandId,
                                                            ParamList? paramList = null,
                                                            List<dynamic>? entities = null,
                                                            IObjectStorage? storage = null)
        {
            if (scriptCommandId == 0)
            {
                string err = $"В {nameof(ExecuteScriptCommandAsync)} передан нулевой Id";
                _logger.LogError(_errFmt, err);
                return Result<dynamic>.Error(err);
            }

            if (!_isInitialized)
            {
                return Result<dynamic>.Error("ScriptCore не инициализирован!");
            }

            string prefix = $"Скрипт-операция Id={scriptCommandId}";
            
            Result<IScriptCommand> resScriptCommand = await GetScriptCommandObjectAsync(scriptCommandId);
            if (resScriptCommand.IsErrorOrNull)
            {
                string err = $"{prefix}: {resScriptCommand.ErrorResult}";
                return Result<dynamic>.Error(err);
            }
            
            AppCore? appCore = await _getAppCoreAsync();
            if (appCore == null)
            {
                string err = $"{prefix}: отказ в выполнении: не удалось получить экземпляр AppCore";
                _logger.LogError(_errFmt, err);
                return Result<dynamic>.Error(err);
            }

            AuditRecord _buildAuditRecord(Result r)
            {
                AuditRecord audit = new()
                {
                    RecordDateTime = DateTime.UtcNow,
                    UserName = appCore.CurrentUser.Login,
                    EntityType = nameof(ScriptCommand),
                    Action = "ExecuteScriptCommand",
                    EntityId = scriptCommandId,
                    KeyValues = paramList?.ToJson() ?? "",
                    Changes = r.ToString()
                };
                return audit;
            }

            // Вспомогательная локальная функция
            Result<dynamic> CheckAndReturn(Result res)
            {
                if(!res.IsError) return Result<dynamic>.SuccessWithMessage(null, res.ErrorResult);
                _logger.LogError("{Prefix}: ошибка при выполнении: {Err}", prefix, res.ErrorResult);
                
                return Result<dynamic>.Error(res); 
            }
            
            // Сначала пробуем выполнить скрипт как синхронный
            if (resScriptCommand.Value is IScriptCommandSync scriptCommandSync)
            {
                Result res = scriptCommandSync.Execute(appCore, paramList, entities, storage);
                await _auditService.WriteAsync(_buildAuditRecord(res));
                return CheckAndReturn(res);
            }

            // Если не прокатило - выполняем скрипт как асинхронный
            if (resScriptCommand.Value is IScriptCommandAsync scriptCommandAsync)
            {
                Result res = await scriptCommandAsync.ExecuteAsync(appCore, paramList, entities, storage);
                await _auditService.WriteAsync(_buildAuditRecord(res));
                return CheckAndReturn(res);
            }

            if (resScriptCommand.Value is IScriptCommandDataSource scriptCommandDataSource)
            {
                Result<List<dynamic>> resList = await scriptCommandDataSource.ExecuteAsync(appCore, paramList, entities, storage);
                await _auditService.WriteAsync(_buildAuditRecord(resList));
                return resList.IsError 
                    ? Result<dynamic>.Error(resList.ErrorResult, resList.Exception) 
                    : Result<dynamic>.SuccessWithMessage(resList.Value, resList.ErrorResult);
            }

            // Сюда попадаем если ни один требуемый интерфейс не реализован
            string strError = $"{nameof(ScriptCommand)} (Id = {scriptCommandId} не реализует интерфейсы " +
                              $"({nameof(IScriptCommandSync)}, {nameof(IScriptCommandAsync)}, {nameof(IScriptCommandDataSource)})!";
            _logger.LogError(_errFmt, strError);
            return Result<dynamic>.Error(strError);
        }

        public Result<dynamic> ExecuteScriptFunction(string code, ScriptExecutionGlobals globals)
        {
            if (!_isInitialized)
            {
                string strError = $"ScriptCore не инициализирован!";
                _logger.LogError(_errFmt, strError);
                return Result<dynamic>.Error(strError);
            }

            if (_varAppCore == null)
            {
                string strError = $"ScriptCore не полностью инициализирован: отсутствует {nameof(AppCore)}!";
                _logger.LogError(_errFmt, strError);
                return Result<dynamic>.Error(strError);
            }
            
            if (string.IsNullOrEmpty(code))
            {
                string strError = $"Код для исполнения отсутствует.";
                _logger.LogError(_errFmt, strError);
                return Result<dynamic>.Error(strError);
            }

            Result<IScriptFunction> resSC = GetScriptFunctionObjectAsync(code, globals);
            if(resSC.IsError) return Result<dynamic>.Error(resSC.ErrorResult);
 
            IScriptFunction? resScriptCommand = resSC.Value;
            if(resScriptCommand == null)
            {
                string strError = $"Вернулся нулевой объект {nameof(IScriptFunction)}";
                _logger.LogError(_errFmt, strError);
                return Result<dynamic>.Error(strError);
            }

            try
            {
                var result = resScriptCommand.Execute(globals, _varAppCore);
                return Result<dynamic>.Success(result);
            }
            catch (Exception ex)
            {
                string strError = $"Исключение при исполнении кода: {code}: {ex.Message}";
                _logger.LogError(ex, _errFmt, strError);
                return Result<dynamic>.Error(strError, ex);
                
            }
        }

        private Result<IScriptFunction> GetScriptFunctionObjectAsync(string code, object globals)
        {
            try
            {
                //todo хэширование ключа вместо code 
                // 1. Проверяем наличие в карте
                bool bExists = mapScriptFunction.TryGetValue(code, out IScriptFunction? data);
                if (bExists && data != null)
                {
                    return Result<IScriptFunction>.Success(data);
                }
                
                // Выполняем компиляцию и запись скрипта в кэш.
                string functionCode = BuildFuncCode(code);
                Result<IScriptFunction> res = CompileScriptFunction(functionCode, globals);
                if(res.IsError) return res;

                IScriptFunction? result = res.Value;
                if(result == null)
                {
                    string strError = $"Код скомпилировался без ошибки, но вернулся {nameof(IScriptFunction)} == null";
                    _logger.LogError(_errFmt, strError);
                    return Result<IScriptFunction>.Error(strError);
                }

                mapScriptFunction[code] = result;

                return Result<IScriptFunction>.Success(result);
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при получении {nameof(IScriptCommand)}: {ex.Message}";
                _logger.LogError(ex, _errFmt, strError);
                return Result<IScriptFunction>.Error(strError, ex);
            }
        }

        private Result<IScriptFunction> CompileScriptFunction(string code, object globals)
        {
            CSharpScriptExecution scriptExecution = new() { SaveGeneratedCode = true };
            scriptExecution.AddDefaultReferencesAndNamespaces();
            scriptExecution.AddLoadedReferences();
            scriptExecution.AddNamespace("Frame.App.Scripting");

            lock (_lockExecutor)
            {
                IScriptFunction scriptFunction = scriptExecution.CompileClass(code);

                if (scriptExecution.Error)
                {
                    string strError = $"Ошибка при получении {nameof(IScriptFunction)}: {scriptExecution.ErrorMessage}";
                    _logger.LogError(_errFmt, strError);
                    return Result<IScriptFunction>.Error(strError);
                }

                return Result<IScriptFunction>.Success(scriptFunction);
            }
        }

        /// <summary>
        /// Инъекция в код, приходящая из отчёта/паспорта типа: Obj.Address, Obj.DateTime.Now().
        /// </summary>
        /// <param name="injection">Инъекция.</param>
        private string BuildFuncCode(string injection)
        {
            string classCode = _using;
            string className = $"C_{Guid.NewGuid()}";
            className = className.Replace("-", "");

            classCode += $"public class {className} : IScriptFunction\n";
            classCode += "{\n";
            classCode += "public dynamic? Execute(ScriptExecutionGlobals globals, AppCore appCore)\n";
            classCode += "{\n";
            classCode += "if(globals == null) \n";
            classCode += "return null;\n";
            classCode += $"return globals?.{injection};\n";
            classCode += "}\n";
            classCode += "}";

            return classCode;
        }

        /// <summary>
        /// В отличие от EntityScript ScriptCommand не загружаются при старте сервера. 
        /// Загрузка осуществляется по требованию, и в момент загрузке сразу же компилируются и сохраняется в кэш.
        /// Поэтому если требуемый ScriptCommand отсутствует в кэше - он компилится и сохраняется.
        /// Если он есть в кэше но не скомпилированный - странная ошибка, удаляем и заново компилим/сохраняем.
        /// </summary>
        /// <returns></returns>
        private async Task<Result<IScriptCommand>> GetScriptCommandObjectAsync(int scriptCommandId)
        {
            try
            {
                lock (_lockMaps)
                {
                    // 1. Проверяем наличие в карте
                    bool bExists = mapScriptCommandData.TryGetValue(scriptCommandId, out ScriptCommandData? data);
                    if (bExists && data != null)
                    {
                        if (data.CompiledCode != null)
                        {
                            IScriptCommand? scriptCommand = data.CompiledCode;
                            if (scriptCommand != null)
                            {
                                // Найдено. Возвращаем готовый скрипт-класс.
                                return Result<IScriptCommand>.Success(scriptCommand);
                            }
                            else
                            {
                                string strError = $"Скомпилированный и сохраненный ранее класс для ScriptCommand (Id = {scriptCommandId}) не поддерживает интерфейс {nameof(IScriptCommand)}. Пересоздаем заново.";
                                _logger.LogError(_errFmt, strError);
                            }
                        }
                        else
                        {
                            // Класс не был скомпилириван. Удаляем его и все делаем заново.
                            string strError = $"Класс ScriptCommand (Id = {scriptCommandId}) не был скомпилирован ранее. Пересоздаем заново.";
                            _logger.LogError(_errFmt, strError);
                        }
                    }
                }
                // Выполняем компиляцию и запись скрипта в кэш.
                return await CompileScriptCommandAsync(scriptCommandId);
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при получении {nameof(IScriptCommand)}: {ex.Message}";
                _logger.LogError(ex, _errFmt, strError);
                return Result<IScriptCommand>.Error(strError, ex);
            }
        }

        private Result<IEntityScriptMethod> GetEntityScriptObject(BaseEntity entity, EHookType hookType)
        {
            try
            {
                string strScriptClassName = EntityScript.Meta.BuildScriptClassName(entity, hookType);
                lock (_lockMaps)
                {
                    // 1. Проверяем наличие в карте
                    bool bExists = mapEntityScriptData.TryGetValue(strScriptClassName, out EntityScriptData? data);
                    if (bExists && data != null)
                    {
                        if(data.CompiledCode != null)
                        {
                            IEntityScriptMethod? entityScriptMethod = data.CompiledCode;
                            if (entityScriptMethod == null)
                            {
                                string strError = $"Скомпилированный и сохраненный ранее класс {strScriptClassName} " +
                                                  $"не поддерживает интерфейс {nameof(IEntityScriptMethod)}";
                                _logger.LogError(_errFmt, strError);
                                return Result<IEntityScriptMethod>.Error(strError);
                            }
                            else
                            {
                                // Найдено. Возвращаем готовый скрипт-класс.
                                return Result<IEntityScriptMethod>.Success(entityScriptMethod);
                            }
                        }
                        else
                        {
                            // Класс еще не был скомпилириван. Компилируем и сохраняем.
                            Result<IEntityScriptMethod> res = EntityScript_CompileAndSaveToMap(strScriptClassName);
                            if (res.IsError)
                            {
                                return Result<IEntityScriptMethod>.Error(res.ErrorResult);
                            }
                            else
                            {
                                return Result<IEntityScriptMethod>.Success(res.Value);
                            }
                        }
                    }
                    else
                    {
                        // Для данного типа и хука нет сохраненных скриптов
                        return Result<IEntityScriptMethod>.Success(null);
                    }
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при получении {nameof(IEntityScriptMethod)}: {ex.Message}";
                _logger.LogError(ex, _errFmt, strError);
                return Result<IEntityScriptMethod>.Error(strError, ex);
            }
        }

        private async Task<Result> InitAsync(IEntityScriptLoader entityScriptLoader)
        {
            try
            {
                // Читаем все EntityScript
                Result<int> res1 = await LoadEntityScriptSourceCodeAsync(entityScriptLoader);
                if (res1.IsError)
                {
                    _logger.LogError("{Message}", res1.ErrorResult);
                    return Result.Error(res1.ErrorResult);
                }
                else
                {
                    _logger.LogInformation("Прочитано и сохранено скриптовых методов - расширений объектов: {0}", res1.Value);
                }

                lock (_lockMaps)
                {
                    _isInitialized = true;
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                string strError = $"Ошибка инициализаии ScriptCore: {ex}";
                _logger.LogError(ex, _errFmt, strError);
                return Result.Error(strError, ex);
            }
        }

        private Result<int> SaveToMap(EntityScript entityScript)
        {
            try
            {
                int iSaved = 0;
                if (entityScript.EntityType.Length == 0)
                {
                    _logger.LogError(_errFmt, $"В объекте {nameof(EntityScript)} id = {entityScript.Id} не задан {nameof(EntityScript.EntityType)}");
                }
                else if (entityScript.ScriptCode.Length == 0)
                {
                    _logger.LogError(_errFmt, $"В объекте {nameof(EntityScript)} id = {entityScript.Id} отсутствует c# код для компиляции");
                }
                else
                {
                    string className = EntityScript.Meta.BuildScriptClassName(entityScript);
                    string code = BuildEntityScriptCode(entityScript, className);
                    EntityScriptData data = new()
                    {
                        SourceCode = code,
                        ThreadType = entityScript.ThreadType
                    };
                    // Прописываем новый исходный код
                    mapEntityScriptData[className] = data;
                    iSaved = 1;
                }
                return Result<int>.Success(iSaved);
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка сохранения кода EntityScript с Id={entityScript.Id} в кэш.";
                _logger.LogError(ex, _errFmt, strError);
                return Result<int>.Error(strError, ex);
            }
        }

        private async Task<Result<ScriptCommandData>> BuildScriptCommandDataAsync(int scriptCommandId)
        {
            if(_objectStorage == null)
            {
                string strErr = $"Не удалось получить IObjectStorage";
                _logger.LogError(_errFmt, strErr);
                return Result<ScriptCommandData>.Error(strErr);
            }

            await InitFrameSettingsAsync();

            Result<ScriptCommand> resScriptCommand = await _objectStorage.GetObjAsync<ScriptCommand>(scriptCommandId);
            if(resScriptCommand.IsErrorOrNull)
            {
                return Result<ScriptCommandData>.Error(resScriptCommand.ErrorResult);
            }

            Result<ScriptCommandData> resData = await BuildScriptCommandDataAsync(resScriptCommand.Value!);
            return resData;
        }

        private async Task<Result<ScriptCommandData>> BuildScriptCommandDataAsync(ScriptCommand scriptCommand)
        {
            await InitFrameSettingsAsync();

            if (scriptCommand.ScriptName.Length == 0)
            {
                string strErr = $"В объекте {nameof(ScriptCommand)} id = {scriptCommand.Id} не задан {nameof(ScriptCommand.ScriptName)}";
                _logger.LogError(_errFmt, strErr);
                return Result<ScriptCommandData>.Error(strErr);
            }
            if (scriptCommand.ScriptCode.Length == 0)
            {
                string strErr = $"В объекте {nameof(ScriptCommand)} id = {scriptCommand.Id} отсутствует c# код для компиляции";
                _logger.LogError(_errFmt, strErr);
                return Result<ScriptCommandData>.Error(strErr);
            }

            // Формируем исходный код в формате, пригодном для компиляции
            string className = ScriptCommand.Meta.BuildScriptClassName(scriptCommand);
            string code = BuildScriptCommandCode(scriptCommand, className);
            ScriptCommandData data = new()
            {
                ClassName = className,
                SourceCode = code,
                ThreadType = scriptCommand.ThreadType,
                CommandType = scriptCommand.CommandType
            };
            return Result<ScriptCommandData>.Success(data);
        }

        private Result<IEntityScriptMethod> EntityScript_CompileAndSaveToMap(string className)
        {
            _logger.LogTrace("Компиляция кода EntityScript");

            try
            {
                bool bCodeExists = mapEntityScriptData.TryGetValue(className, out EntityScriptData? data);
                if (bCodeExists && data != null)
                {
                    Result<IEntityScriptMethod> resObject;
                    if (data.ThreadType == EThreadType.Async)
                    {
                        Result<IEntityScriptMethodAsync> resObjectAsync = CompileScript<IEntityScriptMethodAsync>(className, data.SourceCode);
                        if(resObjectAsync.IsErrorOrNull)
                        {
                            return Result<IEntityScriptMethod>.Error(resObjectAsync.ErrorResult);
                        }
                        resObject = Result<IEntityScriptMethod>.Success(resObjectAsync.Value);
                    }
                    else
                    {
                        Result<IEntityScriptMethodSync> resObjectSync = CompileScript<IEntityScriptMethodSync>(className, data.SourceCode);
                        if (resObjectSync.IsErrorOrNull)
                        {
                            return Result<IEntityScriptMethod>.Error(resObjectSync.ErrorResult);
                        }
                        resObject = Result<IEntityScriptMethod>.Success(resObjectSync.Value);
                    }
                    // Скомпилированный код сохраняем в EntityScriptData и заменяем в карте
                    data.CompiledCode = resObject.Value;
                    mapEntityScriptData[className] = data;
                    return resObject;
                }
                else
                {
                    string strError = $"Ошибка получения кода скрипта {className}";
                    _logger.LogError(strError);
                    return Result<IEntityScriptMethod>.Error(strError);
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка получения из кэша и компиляции кода скрипта {className}";
                _logger.LogError(ex, _errFmt, strError);
                return Result<IEntityScriptMethod>.Error(strError, ex);
            }
        }

        public async Task<Result<IScriptCommand>> CompileScriptCommandAsync(int scriptCommandId, bool saveToMap = true)
        {
            if (scriptCommandId == 0)
            {
                string strError = $"В {nameof(CompileScriptCommandAsync)} передан нулевой Id";
                _logger.LogError(_errFmt, strError);
                return Result<IScriptCommand>.Error(strError);
            }

            if (!_isInitialized)
            {
                string strError = $"{nameof(CompileScriptCommandAsync)}(id = {scriptCommandId}): ScriptCore не инициализирован!";
                _logger.LogError(_errFmt, strError);
                return Result<IScriptCommand>.Error(strError);
            }

            try
            {
                Result<ScriptCommandData> result = await BuildScriptCommandDataAsync(scriptCommandId);
                if(result.IsErrorOrNull)
                {
                    return Result<IScriptCommand>.Error(result.ErrorResult);
                }

                ScriptCommandData data = result.Value!;
                Result<IScriptCommand> resObject = CompileScriptCommand(data);

                if (resObject.IsErrorOrNull)
                {
                    string strError = $"Ошибка компиляции ScriptCommand (Id = {scriptCommandId}) :\n{resObject.ErrorResult}";
                    _logger.LogError(_errFmt, strError);
                }
                else
                {
                    // Скомпилированный код сохраняем в EntityScriptData и заменяем в карте
                    if (saveToMap)
                    {
                        data.CompiledCode = resObject.Value;
                        mapScriptCommandData[scriptCommandId] = data;
                    }
                }
                return resObject;
            }
            catch (Exception ex)
            {
                string strError = $"Непредвиденная ошибка при выполнении {nameof(IScriptCore.CompileScriptCommandAsync)} для ScriptCommand (Id = {scriptCommandId}'";
                _logger.LogError(ex, _errFmt, strError);
                return Result<IScriptCommand>.Error(strError, ex);
            }
        }

        public async Task<Result<IScriptCommand>> CompileScriptCommandAsync(ScriptCommand scriptCommand)
        {
            if (!_isInitialized)
            {
                string strError = $"{nameof(CompileScriptCommandAsync)}({scriptCommand}): ScriptCore не инициализирован!";
                _logger.LogError(_errFmt, strError);
                return Result<IScriptCommand>.Error(strError);
            }

            try
            {
                Result<ScriptCommandData> result = await BuildScriptCommandDataAsync(scriptCommand);
                if(result.IsErrorOrNull)
                {
                    return Result<IScriptCommand>.Error(result.ErrorResult);
                }

                ScriptCommandData data = result.Value!;
                return CompileScriptCommand(data);
            }
            catch (Exception ex)
            {
                string strError = $"Непредвиденная ошибка при выполнении {nameof(IScriptCore.CompileScriptCommandAsync)} для ScriptCommand '{scriptCommand}'";
                _logger.LogError(ex, _errFmt, strError);
                return Result<IScriptCommand>.Error(strError, ex);
            }
        }

        /// <summary>
        /// Компиляция переданного кода без сохранения результата. Применяется для контроля ошибок при редактировании скрипта 
        /// </summary>
        /// <param name="strCode"></param>
        /// <returns></returns>
        public Result CompileCode(string strCode)
        {
            try
            {
                var script = new CSharpScriptExecution() { SaveGeneratedCode = true };
                script.AddDefaultReferencesAndNamespaces();
                script.AddLoadedReferences();

                script.CompileClass(strCode);

                if (script.Error)
                {
                    return Result.Error(script.ErrorMessage);
                }
                else
                {
                    return Result.Success;
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка при компиляции: {ex.Message}";
                return Result.Error(strError, ex);
            }
        }

        private Result<TRunInterface> CompileScript<TRunInterface>(string className, string strCode) where TRunInterface : class
        {
            try
            {
                var script = new CSharpScriptExecution() { SaveGeneratedCode = true };
                script.AddDefaultReferencesAndNamespaces();
                script.AddLoadedReferences();

                dynamic xObject = script.CompileClass(strCode);

                if (script.Error)
                {
                    string strError = $"Ошибка компиляции скрипта {className}:\n{script.ErrorMessage}";
                    _logger.LogError("{Err} : {Message}", strError, script.ErrorMessage);
                    return Result<TRunInterface>.Error(strError);
                }
                else
                {
                    if (xObject is TRunInterface run)
                    {
                        return Result<TRunInterface>.Success(run);
                    }
                    else
                    {
                        string strError = $"Скопмилированный скрипт {className} не поддерживает интерфейс {typeof(TRunInterface).Name}";
                        _logger.LogError(_errFmt, strError);
                        return Result<TRunInterface>.Error(strError);
                    }
                }
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка компиляции и сохранения в кэше скрипта {className}";
                _logger.LogError(ex, _errFmt, strError);
                return Result<TRunInterface>.Error(strError, ex);
            }
        }

        private Result<IScriptCommand> CompileScriptCommand(ScriptCommandData data)
        {
            Result<IScriptCommand> resObject;
            if (data.CommandType == ECommandType.DataSource)
            {
                Result<IScriptCommandDataSource> resObjectAsync = CompileScript<IScriptCommandDataSource>(data.ClassName, data.SourceCode);
                resObject = resObjectAsync.IsErrorOrNull 
                    ? Result<IScriptCommand>.Error(resObjectAsync.ErrorResult) 
                    : Result<IScriptCommand>.Success(resObjectAsync.Value);
            }
            else if (data.ThreadType == EThreadType.Async)
            {
                Result<IScriptCommandAsync> resObjectAsync = CompileScript<IScriptCommandAsync>(data.ClassName, data.SourceCode);
                resObject = resObjectAsync.IsErrorOrNull 
                    ? Result<IScriptCommand>.Error(resObjectAsync.ErrorResult) 
                    : Result<IScriptCommand>.Success(resObjectAsync.Value);
            }
            else
            {
                Result<IScriptCommandSync> resObjectSync = CompileScript<IScriptCommandSync>(data.ClassName, data.SourceCode);
                resObject = resObjectSync.IsErrorOrNull 
                    ? Result<IScriptCommand>.Error(resObjectSync.ErrorResult) 
                    : Result<IScriptCommand>.Success(resObjectSync.Value);
            }

            return resObject;
        }


        private async Task<Result<int>> LoadEntityScriptSourceCodeAsync(IEntityScriptLoader entityScriptLoader)
        {
            Result<List<EntityScript>> resListMethods = await entityScriptLoader.LoadAllAsync();
            if (resListMethods.IsErrorOrNull)
            {
                return Result<int>.Error(resListMethods.ErrorResult, 0);
            }

            await InitFrameSettingsAsync();

            int iCount = 0;

            lock (_lockMaps)
            {
                foreach (EntityScript es in resListMethods.Value!)
                {
                    Result res = SaveToMap(es);
                    if (!res.IsError)
                    {
                        iCount++;
                    }
                    else
                    {
                        // Здесь ошибку можно пропустить, потому что все что случилось - уже в логе. Просто пропускаем данный EntityScript
                    }
                }
            }
            return Result<int>.Success(iCount);
        }

        private string BuildEntityScriptCode(EntityScript entityScript, string className = "")
        {
            if(className.Length == 0)
            {
                className = EntityScript.Meta.BuildScriptClassName(entityScript);
            }
            string code = _using;

            if (entityScript.CodeType == ECodeType.SimpleMethod)
            {
                if(entityScript.ThreadType == EThreadType.Sync)
                {
                    // Есть простой синхронный метод
                    code += $"public class {className} : IEntityScriptMethodSync\n";
                    code += "{\n";
                    code += SyncMethodEntityScript;
                    code += "\n{\n";
                    code += entityScript.ScriptCode;
                    code += "\n}\n";
                }
                else
                {
                    // Есть простой асинхронный метод
                    code += $"public class {className} : IEntityScriptMethodAsync\n";
                    code += "{\n";
                    code += AsyncMethodEntityScript;
                    code += "\n{\n";
                    code += entityScript.ScriptCode;
                    code += "\n}\n";
                }
                code += "}\n";
            }
            else
            {
                // Есть полностью готовый объект (реализация интерфейса IEntityScriptMethod/Sync/Async)
                code = entityScript.ScriptCode;
            }

            return code;
        }

        private string BuildScriptCommandCode(ScriptCommand scriptCommand, string className = "")
        {
            if (className.Length == 0)
            {
                className = ScriptCommand.Meta.BuildScriptClassName(scriptCommand);
            }
            string code = _using;

            if (scriptCommand.CodeType == ECodeType.SimpleMethod)
            {
                if (scriptCommand.ThreadType == EThreadType.Sync)
                {
                    // Есть простой синхронный метод
                    code += $"public class {className} : IScriptCommandSync\n";
                    code += "{\n";
                    code += SyncMethodScriptCommand;
                    code += "\n{\n";
                    code += scriptCommand.ScriptCode;
                    code += "\n}\n";
                }
                else
                {
                    // Есть простой асинхронный метод
                    code += $"public class {className} : IScriptCommandAsync\n";
                    code += "{\n";
                    code += AsyncMethodScriptCommand;
                    code += "\n{\n";
                    code += scriptCommand.ScriptCode;
                    code += "\n}\n";
                }
                code += "}\n";
            }
            else
            {
                // Есть полностью готовый объект (реализация интерфейса IEntityScriptMethod/Sync/Async)
                code = scriptCommand.ScriptCode;
            }

            return code;
        }

        private const string SyncMethodEntityScript = "public Result Execute(AppCore appCore, BaseEntity obj)";
        private const string AsyncMethodEntityScript = "public async Task<Result> ExecuteAsync(AppCore appCore, BaseEntity obj)";

        private const string SyncMethodScriptCommand = "public Result Execute(AppCore appCore, ParamList? paramList = null, List<dynamic>? entities = null, IObjectStorage? storage = null)";
        private const string AsyncMethodScriptCommand = "public async Task<Result> ExecuteAsync(AppCore appCore, ParamList? paramList = null, List<dynamic>? entities = null, IObjectStorage? storage = null)";

        private string _using
        {
            get
            {
                if (_frameSettings != null && _frameSettings.DefaultUsing.Length > 0)
                {
                    return _frameSettings.DefaultUsing;
                }
                else
                {
                    return """
using System.Collections.Generic;
using System.Threading.Tasks;
using Frame.Shared;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Entities.Test;
using Frame.Domain.Params;
using Frame.App;
using Frame.App.Scripting;
using Frame.App.Cores;
using Frame.App.IEntityRepositories;

#nullable enable


""";
                }
            }
        }
    }

    public class EntityScriptData
    {
        public string SourceCode { get; set; } = "";
        public EThreadType ThreadType { get; set; }
        public IEntityScriptMethod? CompiledCode { get; set; }
    }

    public class ScriptCommandData
    {
        public string ClassName { get; set; } = "";
        public string SourceCode { get; set; } = "";
        public EThreadType ThreadType { get; set; }
        public ECommandType CommandType { get; set; }
        public IScriptCommand? CompiledCode { get; set; }
        public ParamList? Parameters { get; set; }
    }
}
