using TicketPeak.Modules.Ordering.Domain;

namespace TicketPeak.Modules.Ordering.Infrastructure;

internal sealed class OrderRepository(InMemoryOrderingStore store, InMemoryOrderingSession session) : IOrderRepository
{
    public Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken) =>
        Task.FromResult(store.Orders.GetValueOrDefault(id));

    public Task<bool> ExistsForHoldAsync(HoldId holdId, CancellationToken cancellationToken) =>
        Task.FromResult(store.Orders.Values.Any(order => order.HoldId == holdId));

    public void Add(Order order) => session.Enlist(() => store.Orders[order.Id] = order);
}
