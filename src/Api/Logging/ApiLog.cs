namespace SbomQualityGate.Api.Logging;

internal static partial class ApiLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Unhandled exception for {Method} {Path}")]
    public static partial void UnhandledException(
        ILogger logger,
        string method,
        string path,
        Exception? ex);
}
