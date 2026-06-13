using Frame.App.IEntityLoaders;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.App.Scripting

{
    /// <summary>
    /// Интерфейс скрипт-ядра, который используется для выполнения скриптов, как расширения объектов, так и универсальные скрипты.
    /// </summary>
    public interface IScriptCore
    {
        /// <summary>
        /// Инициализация скриптового ядра: загрузка всех доступных исходных кодов (без компиляции). Вызов реализован с использованием <see cref="ScriptCoreStartup"/>
        /// </summary>
        /// <param name="entityScriptLoader">Объект-загрузчик, который позволяет считать необходимые данные из БД вне зависимости от текущего пользователя</param>
        /// <returns></returns>
        public Task<Result> InitializeAsync(IEntityScriptLoader entityScriptLoader);

        /// <summary>
        /// Принудительная перезагрузка системных настроек
        /// </summary>
        /// <returns></returns>
        public Task<Result> ReloadFrameSettingsAsync();
        
        /// <summary>
        /// Очистка всех сохраненных данных в кэше скриптов.
        /// </summary>
        public void Reset();
        
        /// <summary>
        /// Перезагрузка конкретного скрипта при изменении или пересохранении объекта <see cref="EntityScript"/>
        /// </summary>
        public Result ReloadEntityScript<TEntity>(EntityScript script) where TEntity : BaseEntity, new();
        
        /// <summary>
        /// Удаление конкретного скрипта при удалении объекта <see cref="EntityScript"/>
        /// </summary>
        public Result RemoveEntityScript(EntityScript script);
        
        /// <summary>
        /// Перезагрузка конкретного скрипта при изменении или пересохранении объекта <see cref="ScriptCommand"/>. 
        /// Поиск осуществляется по Id чтобы гарантировать полное соответствие скомпилированного кода сохраненной команде.
        /// </summary>
        /// <param name="scriptCommandId">Id команды</param>
        /// <returns></returns>
        public Task<Result> ReloadScriptCommandAsync(int scriptCommandId);
        
        /// <summary>
        /// Удаление конкретного скрипта при удалении объекта <see cref="ScriptCommand"/>
        /// </summary>
        public Result RemoveScriptCommand(int scriptCommandId);
        
        /// <summary>
        /// Компиляция ScriptCommand. При успешной компиляции и saveToMap = true - сохранение результата компиляции
        /// в карте и кэше IScriptCore. Компиляция проводится только по команде, уже сохраненной в БД,
        /// чтобы избежать попадания в кэш кода, который в итоге не был сохранен.
        /// </summary>
        /// <param name="scriptCommandId">Id сохраненной команды</param>
        /// <param name="saveToMap">true - сохранение в кэше, false - без сохранения.</param>
        /// <returns>Результат компиляции. В случае ошибок - в ErrorResult вернутся ошибки компиляции.</returns>
        public Task<Result<IScriptCommand>> CompileScriptCommandAsync(int scriptCommandId, bool saveToMap = true);

        /// <summary>
        /// Компиляция ScriptCommand без сохранения в кэше. Запись ошибок в лог также не производится.
        /// Метод предназначен для отладочной компиляции кода команды, которая даже может быть и не сохранена в БД.
        /// </summary>
        /// <param name="scriptCommand"></param>
        /// <returns></returns>
        public Task<Result<IScriptCommand>> CompileScriptCommandAsync(ScriptCommand scriptCommand);

        /// <summary>
        /// Компиляция кода в целях отладки и устранения ошибок. Запись ошибок в лог не производится.
        /// </summary>
        /// <param name="strCode">Код, подлежащий компиляции и проверке на наличие ошибок</param>
        /// <returns></returns>
        public Result CompileCode(string strCode);
        
        /// <summary>
        /// Поиск и выполнение скрипта-расширения методов моделей OnBefore/After/Save/Delete/Async. Если расширения не существует - просто вернется Result.Success.
        /// </summary>
        /// <param name="entity">Объект, скрипты-расширения которого нужно выполнить.</param>
        /// <param name="hookType">Тип расширения, который нужно получить и выполнить.</param>
        /// <param name="threadType">Реализация вызываемого метода: синхронный или асинхронный.</param>
        /// <returns></returns>
        public Task<Result> ExecuteEntityScriptAsync(BaseEntity entity, EHookType hookType, EThreadType threadType);

        /// <summary>
        /// Запуск <see cref="ScriptCommand"/> на исполнение. При отсутствии команды в кэше осуществляется чтение, компиляция и сохранение. 
        /// В качестве аргументя принимается именно Id а не готовый объект, чтобы гарантировать полное соответствие скомпилированного кода сохраненной в БД команде.
        /// </summary>
        /// <param name="scriptCommandId">Id команды</param>
        /// <param name="paramList">Список параметров, предварительно заполненный пользователем (при необходимости)</param>
        /// <param name="entities">Выбранные пользователем объекты для случая, когда команда выполняется над списком объектов</param>
        /// <param name="storage">Хранилище, из которого были прочитаны объекты. Пересохранять объекты нужно в то же хранилище, откуда они были прочитаны.</param>
        /// <returns>Для обычного ScriptCommand-а - возвращается просто Result, для DataSource - Result{List{dynamic}}</returns>
        public Task<Result<dynamic>> ExecuteScriptCommandAsync(int scriptCommandId, 
                                                               ParamList? paramList = null, 
                                                               List<dynamic>? entities = null, 
                                                               IObjectStorage? storage = null);
        /// <summary>
        /// Запуск на исполение функции вида Obj.Address, Obj.DateTime.Today(). При отсутствии функции в кэше осуществляется чтение, компиляция и сохранение.
        /// В качестве аргумента для поиска в кэше используется код самой функции.
        /// </summary>
        /// <param name="code">Код функции.</param>
        /// <param name="globals"><Объект, к которому применяется функция.</param>
        /// <param name="paramList">Список параметров, предварительно заполненный пользователем (при необходимости)</param>
        /// <returns>Возвращает результат выполнения функции.</returns>
        public Result<dynamic> ExecuteScriptFunction(string code, ScriptExecutionGlobals globals);
    }

}
