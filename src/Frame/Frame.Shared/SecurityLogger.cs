using Microsoft.Extensions.Logging;

namespace Frame.Shared
{
    public static class LoggerExtensions
    {
        /// <summary>
        /// Логгирование с добавлением префикса "Security: ". В результате данное сообщение будет выведено в отдельный лог Security.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        /// <param name="ex"></param>
        public static void LogSecurity(this ILogger logger, string message, LogLevel logLevel = LogLevel.Information, Exception? ex = null)
        {
            ArgumentNullException.ThrowIfNull(logger);

            string messageType = "Security";
            logger.Log(logLevel, ex, "{MessageType}: {message}", messageType, message);
        }


        /// <summary>
        /// Логгирование результата из <paramref name="result"/>. Если result.IsError == false - сообщение не логгируется.
        /// Структура итогового сообщения: <paramref name="message"/>: <paramref name="result.ErrorResult"/>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        /// <param name="result"></param>
        /// <param name="logLevel"></param>
        public static void LogResult(this ILogger logger, string message, Result result, LogLevel logLevel = LogLevel.Error)
        {
            ArgumentNullException.ThrowIfNull(logger);

            if (result == null)
            {
                logger.Log(logLevel, message);
                return;
            }

            if(!result.IsError)
            {
                return;
            }

            string strMessage = (message.Length == 0) ? result.ErrorResult : $"{message}: {result.ErrorResult}";
            if(result.Exception != null)
            {
                logger.Log(logLevel, result.Exception, "{message}: {ErrorResult}", strMessage, result.ErrorResult);
            }
            else
            {
                logger.Log(logLevel, "{message}: {ErrorResult}", strMessage, result.ErrorResult);
            }
        }

        public static void LogWarning(this ILogger logger, string message, Result result) => logger.LogResult(message, result, LogLevel.Warning);
        public static void LogError(this ILogger logger, string message, Result result) => logger.LogResult(message, result, LogLevel.Error);
        public static void LogCritical(this ILogger logger, string message, Result result) => logger.LogResult(message, result, LogLevel.Critical);
    }
}
