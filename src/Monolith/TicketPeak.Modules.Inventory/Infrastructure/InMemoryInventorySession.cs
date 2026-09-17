using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Infrastructure;

/// <summary>
/// Inventory's per-request unit of work: new aggregates reach the store only when the pipeline
/// commits, so a refused command leaves nothing behind.
/// </summary>
internal sealed class InMemoryInventorySession : IUnitOfWork
{
    private readonly List<Action> _pendingAdds = [];

    public void Enlist(Action add) => _pendingAdds.Add(add);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (Action add in _pendingAdds)
        {
            add();
        }

        _pendingAdds.Clear();
        return Task.CompletedTask;
    }
}
