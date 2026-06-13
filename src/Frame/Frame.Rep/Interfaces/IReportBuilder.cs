using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Params;
using Frame.Shared;

namespace Frame.Rep.Interfaces
{
    public interface IReportBuilder
    {
        public Task<Result<MemoryStream>> GenerateReportAsync(FrameReport report, 
            ParamList paramList, List<dynamic>? entities = null, IObjectStorage? storage = null);
    }
}
