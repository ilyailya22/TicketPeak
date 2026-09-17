using System.Collections.Concurrent;
using TicketPeak.Modules.Catalog.Domain;

namespace TicketPeak.Modules.Catalog.Infrastructure;

/// <summary>
/// Phase 1 stand-in for the database, replaced by EF Core in Phase 2. Two honest limitations: a
/// loaded aggregate is the stored instance, so its changes are visible before the unit of work
/// commits; and aggregates are not thread-safe, so concurrent requests against the same one are
/// not protected. Phase 2's concurrency tests exist precisely because this cannot pass them.
/// </summary>
internal sealed class InMemoryCatalogStore
{
    public ConcurrentDictionary<VenueId, Venue> Venues { get; } = new();

    public ConcurrentDictionary<EventId, Event> Events { get; } = new();
}
