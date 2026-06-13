using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.App.Cores
{
    /// <summary>
    /// Доступ к параметрам сервера и служебным методам
    /// </summary>
    public interface IServerCore
    {
        public Result ReloadSystemParamList();
        public ParamList SystemParamList { get; }
    }
}
