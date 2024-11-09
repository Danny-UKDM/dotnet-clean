using Microsoft.Extensions.Logging;

namespace Application.Common;

public static partial class LoggerMessages
{
    [LoggerMessage(LogLevel.Error, "{ExceptionName} thrown while executing {Type} with Message: '{Message}'")]
    public static partial void LogGenericException(this ILogger logger, Exception ex, string exceptionName, string type, string message);

    [LoggerMessage(LogLevel.Error, "Retryable {ExceptionName} thrown while executing {Type} with Message: '{Message}'")]
    public static partial void LogRetryableException(this ILogger logger, Exception ex, string exceptionName, string type, string message);
}
