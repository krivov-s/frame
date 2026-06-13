using Frame.App.Cores;
using Frame.Domain.Params;

namespace Frame.App.Scripting
{
    /// <summary>
    /// Интерфейс для выполнения скриптов в отчётах/паспортах типа: Obj.Name, Obj.DateTime.Now()
    /// </summary>
    public interface IScriptFunction
    {
        public dynamic? Execute(ScriptExecutionGlobals globals, AppCore appCore);
    }
}
