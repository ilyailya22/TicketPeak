using TicketPeak.Modules.Catalog.Domain;

namespace TicketPeak.Modules.Catalog.Infrastructure;

internal sealed class VenueRepository(InMemoryCatalogStore store, InMemoryCatalogSession session) : IVenueRepository
{
    public Task<Venue?> FindAsync(VenueId id, CancellationToken cancellationToken) =>
        Task.FromResult(store.Venues.GetValueOrDefault(id));

    public void Add(Venue venue) => session.Enlist(() => store.Venues[venue.Id] = venue);
}
