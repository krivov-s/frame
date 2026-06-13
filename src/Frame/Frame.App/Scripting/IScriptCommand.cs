
namespace Frame.App.Scripting
{
    /// <summary>
    /// Базовый интерфейс для скомпилированной <see cref="ScriptCommand"/>.
    /// Для реального исполнения используются интерфейсы <see cref="IScriptCommandSync"/> или <see cref="IScriptCommandAsync"/>
    /// </summary>
    public interface IScriptCommand
    {
    }
}
