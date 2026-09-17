namespace TicketPeak.Modules.Catalog.Domain;

internal interface IEventRepository
{
    Task<Event?> FindAsync(EventId id, CancellationToken cancellationToken);

    /// <summary>Stages a new event; it is stored when the unit of work commits.</summary>
    void Add(Event @event);
}
