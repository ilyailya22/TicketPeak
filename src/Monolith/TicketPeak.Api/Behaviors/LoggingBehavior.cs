using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Api.Behaviors;

/// <summary>
/// Records every request and its outcome. A refused business rule is logged as a warning with its
/// error code, not as an error: it is an expected outcome, and alerting on it would be noise.
/// Exceptions are left to propagate; turning them into responses is the API edge's job.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly string _requestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        PipelineLog.Handling(logger, _requestName);

        TResponse response = await next(cancellationToken);

        if (response is Result result && result.IsFailure)
        {
            PipelineLog.Refused(logger, _requestName, result.Error.Code, result.Error.Message);
        }
        else
        {
            PipelineLog.Handled(logger, _requestName);
        }

        return response;
    }
}
