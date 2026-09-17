using TicketPeak.Shared.Kernel;

namespace TicketPeak.Api.Tests.Behaviors;

internal sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
