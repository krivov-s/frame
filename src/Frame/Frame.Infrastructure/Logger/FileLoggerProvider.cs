using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.Logger
{
    /// <summary>
    /// DEPRECATED: заменен на Serilog
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _strPath;
        public FileLoggerProvider(string strPath)
        {
            _strPath = strPath;
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_strPath);
        }

        public void Dispose()
        {
        }
    }
}
