using TicketPeak.Modules.Inventory.Domain;

namespace TicketPeak.Modules.Inventory.Infrastructure;

internal sealed class EventInventoryRepository(InMemoryInventoryStore store, InMemoryInventorySession session)
    : IEventInventoryRepository
{
    public Task<EventInventory?> FindAsync(EventId id, CancellationToken cancellationToken) =>
        Task.FromResult(store.Inventories.GetValueOrDefault(id));

    public void Add(EventInventory inventory) => session.Enlist(() => store.Inventories[inventory.Id] = inventory);
}
