using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.Logger
{
    /// <summary>
    /// DEPRECATED: заменен на Serilog
    /// </summary>
    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath)
        {
            builder.AddProvider(new FileLoggerProvider(filePath));
            return builder;
        }
    }
}
