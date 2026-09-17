using MediatR;
using TicketPeak.Modules.Ordering.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Application.GetOrder;

internal sealed class GetOrderHandler(IOrderRepository orders) : IRequestHandler<GetOrderQuery, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        Order? order = await orders.FindAsync(new OrderId(request.OrderId), cancellationToken);

        if (order is null)
        {
            return OrderingApplicationErrors.OrderNotFound;
        }

        return new OrderResponse(
            order.Id.Value,
            order.Status.ToString(),
            order.Total.AmountMinor,
            order.Total.Currency.Value,
            order.ExpiresAt,
            [.. order.Lines.Select(line => line.Ticket)]);
    }
}
