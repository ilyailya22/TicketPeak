namespace TicketPeak.Api.Behaviors;

/// <summary>
/// Source-generated log messages for the pipeline. The generator emits an IsEnabled check and a
/// strongly typed state struct, so a disabled level costs nothing: no argument evaluation, no
/// boxing, no params array. This runs on every request, which is exactly where that matters.
/// </summary>
internal static partial class PipelineLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    public static partial void Handling(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName}")]
    public static partial void Handled(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestName} was refused: {ErrorCode} {ErrorMessage}")]
    public static partial void Refused(ILogger logger, string requestName, string errorCode, string errorMessage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestName} took {ElapsedMilliseconds} ms, over the {ThresholdMilliseconds} ms threshold")]
    public static partial void SlowRequest(ILogger logger, string requestName, double elapsedMilliseconds, double thresholdMilliseconds);
}
