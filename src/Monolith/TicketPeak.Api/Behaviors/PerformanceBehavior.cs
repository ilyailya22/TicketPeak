using MediatR;

namespace TicketPeak.Api.Behaviors;

/// <summary>
/// Warns when a request takes longer than <see cref="Threshold"/>. A cheap early signal that
/// survives until Phase 5 replaces it with proper latency metrics. Timed with
/// <see cref="TimeProvider"/> so tests can prove the threshold without sleeping.
/// </summary>
internal sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    TimeProvider time)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly string _requestName = typeof(TRequest).Name;

    public static TimeSpan Threshold { get; } = TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        long startedAt = time.GetTimestamp();

        TResponse response = await next(cancellationToken);

        TimeSpan elapsed = time.GetElapsedTime(startedAt);

        if (elapsed > Threshold)
        {
            PipelineLog.SlowRequest(logger, _requestName, elapsed.TotalMilliseconds, Threshold.TotalMilliseconds);
        }

        return response;
    }
}
