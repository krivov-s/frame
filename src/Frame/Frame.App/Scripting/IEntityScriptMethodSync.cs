using Frame.App.Cores;
using Frame.Domain.Entities.Core;
using Frame.Shared;

namespace Frame.App.Scripting
{
    public interface IEntityScriptMethodSync : IEntityScriptMethod
    {
        public Result Execute(AppCore appCore, BaseEntity obj);
    }
}
