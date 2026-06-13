using Frame.App.Cores;
using Frame.Domain.Entities.Core;
using Frame.Shared;

namespace Frame.App.Scripting
{
    public interface IEntityScriptMethodAsync : IEntityScriptMethod
    {
        public Task<Result> ExecuteAsync(AppCore appCore, BaseEntity obj);
    }
}
