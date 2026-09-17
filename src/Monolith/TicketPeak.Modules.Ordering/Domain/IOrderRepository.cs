namespace TicketPeak.Modules.Ordering.Domain;

internal interface IOrderRepository
{
    Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken);

    Task<bool> ExistsForHoldAsync(HoldId holdId, CancellationToken cancellationToken);

    /// <summary>Stages a new order; it is stored when the unit of work commits.</summary>
    void Add(Order order);
}
