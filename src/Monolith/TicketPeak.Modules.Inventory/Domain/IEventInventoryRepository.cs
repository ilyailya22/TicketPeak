namespace TicketPeak.Modules.Inventory.Domain;

internal interface IEventInventoryRepository
{
    Task<EventInventory?> FindAsync(EventId id, CancellationToken cancellationToken);

    /// <summary>Stages a new event inventory; it is stored when the unit of work commits.</summary>
    void Add(EventInventory inventory);
}
