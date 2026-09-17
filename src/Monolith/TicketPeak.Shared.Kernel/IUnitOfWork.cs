namespace TicketPeak.Shared.Kernel;

/// <summary>
/// Commits everything a command changed in one module. Each module provides its own; the pipeline
/// commits all of them after a command succeeds and none of them after it fails.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
