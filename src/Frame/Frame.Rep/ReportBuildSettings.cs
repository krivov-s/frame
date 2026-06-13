using Frame.App.Cores;
using Frame.App.FileRepositories;
using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.Rep.Interfaces;
using Microsoft.Extensions.Logging;

namespace Frame.Rep
{
    public class ReportBuildSettings
    {
        public required ILogger<IFrameReporting> Logger { get; set; }
        public required IFilesRepository FilesRepository { get; set; }
        public required AppCoreProvider AppCoreProvider { get; set; }
        public required IObjectStorageProvider ObjectStorageProvider { get; set; }
        public required IScriptCore ScriptCore {  get; set; }
    }
}
