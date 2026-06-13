using Frame.App.Cores;
using Frame.App.IEntityRepositories;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.App.Scripting
{
    public interface IScriptCommandAsync : IScriptCommand
    {
        public Task<Result> ExecuteAsync(AppCore appCore, 
                                         ParamList? paramList = null,
                                         List<dynamic>? entities = null, 
                                         IObjectStorage? storage = null);
    }
}
