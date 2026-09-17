namespace TicketPeak.Api.Tests.Behaviors;

/// <summary>
/// Stands in for the rest of the pipeline: counts calls, records the token it was given, and
/// returns a fixed response.
/// </summary>
internal sealed class HandlerSpy<TResponse>(TResponse response)
{
    public int Calls { get; private set; }

    public CancellationToken ReceivedToken { get; private set; }

    public Task<TResponse> Invoke(CancellationToken cancellationToken)
    {
        Calls++;
        ReceivedToken = cancellationToken;
        return Task.FromResult(response);
    }
}
