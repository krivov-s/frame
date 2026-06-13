using Frame.Domain.Entities.Core.Reports;
using Frame.Shared;

namespace Frame.Rep.Interfaces
{
    internal interface IFileDocumentLoader
    {
        public Task<Result<byte[]>> LoadFileDocumentsByOwnerKey(FrameReport report, string ownerKey);
    }
}
