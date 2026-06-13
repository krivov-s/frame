using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.Logger
{
    /// <summary>
    /// DEPRECATED: заменен на Serilog
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _strPath;
        private static readonly object _lock = new();
        public FileLogger(string strPath) 
        { 
            _strPath = strPath;
        }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;


        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter != null)
            {
                lock (_lock)
                {
                    string fullFilePath = Path.Combine(_strPath, DateTime.UtcNow.ToString("yyyy-MM-dd") + "_log.txt");
                    string nl = Environment.NewLine;
                    string exc = "";
                    if (exception != null)
                    {
                        exc = nl + exception.GetType() + ": " + exception.Message + nl + exception.StackTrace + nl;
                    }
                    File.AppendAllText(fullFilePath, logLevel.ToString() + ": " + DateTime.UtcNow.ToString("") + " " + formatter(state, exception) + nl + exc);
                }
            }
        }
    }
}
