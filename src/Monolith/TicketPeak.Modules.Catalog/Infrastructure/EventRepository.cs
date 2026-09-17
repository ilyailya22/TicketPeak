using TicketPeak.Modules.Catalog.Domain;

namespace TicketPeak.Modules.Catalog.Infrastructure;

internal sealed class EventRepository(InMemoryCatalogStore store, InMemoryCatalogSession session) : IEventRepository
{
    public Task<Event?> FindAsync(EventId id, CancellationToken cancellationToken) =>
        Task.FromResult(store.Events.GetValueOrDefault(id));

    public void Add(Event @event) => session.Enlist(() => store.Events[@event.Id] = @event);
}
