using Frame.App.Cores;
using Frame.App.IEntityRepositories;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.App.Scripting
{
    public interface IScriptCommandDataSource : IScriptCommand
    {
        public Task<Result<List<dynamic>>> ExecuteAsync(AppCore appCore, 
                                                        ParamList? paramList = null,
                                                        List<dynamic>? entities = null, 
                                                        IObjectStorage? storage = null);
                                         
    }
}
