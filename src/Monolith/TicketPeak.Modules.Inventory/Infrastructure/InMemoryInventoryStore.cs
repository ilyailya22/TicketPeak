using System.Collections.Concurrent;
using TicketPeak.Modules.Inventory.Domain;

namespace TicketPeak.Modules.Inventory.Infrastructure;

/// <summary>
/// Phase 1 stand-in for the database, replaced by EF Core in Phase 2. It is NOT safe under
/// concurrent requests for the same event: EventInventory is not thread-safe, so two simultaneous
/// holds could both succeed for one seat. The single-threaded domain tests prove the invariant;
/// Phase 2's fifty-parallel-buyers test against SQL is what proves it under load.
/// </summary>
internal sealed class InMemoryInventoryStore
{
    public ConcurrentDictionary<EventId, EventInventory> Inventories { get; } = new();
}
