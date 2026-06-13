using System.Dynamic;
using Frame.App.Cores;
using Frame.Domain.Params;

namespace Frame.App.Scripting
{
    /// <summary>
    /// Класс, используемый в качестве хранилаща глобальных переменных при выполнении скриптов в runtime.
    /// </summary>
    public class ScriptExecutionGlobals
    {
        /// <summary>
        /// Основной объект, над которым выполняются действия в скрипте.
        /// </summary>
        public dynamic? Obj { get; set; }

        /// <summary>
        /// Коллекция дополнительных параметров.
        /// </summary>
        public Dictionary<string, dynamic?>? ParamList { get; set; }

        /// <summary>
        /// Ядро приложения.
        /// </summary>
        public AppCore? AppCore { get; set; }
    }
}
