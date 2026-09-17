using System.Collections.Concurrent;
using TicketPeak.Modules.Ordering.Domain;

namespace TicketPeak.Modules.Ordering.Infrastructure;

/// <summary>
/// Phase 1 stand-in for the database, replaced by EF Core in Phase 2. The duplicate-order check
/// reads committed orders only, so two simultaneous requests for one hold are not protected; a
/// unique index on the hold id is what closes that gap in Phase 2.
/// </summary>
internal sealed class InMemoryOrderingStore
{
    public ConcurrentDictionary<OrderId, Order> Orders { get; } = new();
}
