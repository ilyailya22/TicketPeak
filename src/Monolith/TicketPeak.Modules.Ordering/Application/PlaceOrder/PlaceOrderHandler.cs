using System.Globalization;
using MediatR;
using TicketPeak.Modules.Catalog;
using TicketPeak.Modules.Inventory;
using TicketPeak.Modules.Ordering.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Application.PlaceOrder;

/// <summary>
/// Turns a live hold into an order priced from Catalog. Both reads are synchronous calls through
/// the other modules' public APIs, because they sit inside the checkout's consistency boundary.
/// Placing the order does not sell the tickets: the hold is confirmed when payment succeeds.
/// </summary>
internal sealed class PlaceOrderHandler(
    IInventoryApi inventory,
    ICatalogApi catalog,
    IOrderRepository orders,
    TimeProvider time)
    : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        HoldId holdId = new(request.HoldId);

        if (await orders.ExistsForHoldAsync(holdId, cancellationToken))
        {
            return OrderingApplicationErrors.HoldAlreadyOrdered;
        }

        Result<ActiveHold> hold = await inventory.GetActiveHoldAsync(request.EventId, request.HoldId, cancellationToken);

        if (hold.IsFailure)
        {
            return hold.Error;
        }

        Result<EventPricing> pricing = await catalog.GetPricingAsync(request.EventId, cancellationToken);

        if (pricing.IsFailure)
        {
            return pricing.Error;
        }

        Result<List<OrderLine>> lines = LinesFor(hold.Value, pricing.Value);

        if (lines.IsFailure)
        {
            return lines.Error;
        }

        OrderId id = new(Guid.CreateVersion7(time.GetUtcNow()));
        Result<Order> order = Order.Place(
            id,
            new CustomerId(request.CustomerId),
            new EventId(request.EventId),
            holdId,
            lines.Value,
            hold.Value.ExpiresAt,
            time);

        if (order.IsFailure)
        {
            return order.Error;
        }

        orders.Add(order.Value);
        return id.Value;
    }

    private static Result<List<OrderLine>> LinesFor(ActiveHold hold, EventPricing pricing)
    {
        Result<CurrencyCode> currency = CurrencyCode.Create(pricing.Currency);

        if (currency.IsFailure)
        {
            return currency.Error;
        }

        List<OrderLine> lines = [];

        foreach (HeldSeat seat in hold.Seats)
        {
            string ticket = string.Create(CultureInfo.InvariantCulture, $"{seat.Section} {seat.Row}-{seat.Number}");
            Result<OrderLine> line = LineFor(ticket, seat.Section, pricing, currency.Value);

            if (line.IsFailure)
            {
                return line.Error;
            }

            lines.Add(line.Value);
        }

        foreach (HeldStandingPlaces standing in hold.Standing)
        {
            Result<OrderLine> line = LineFor(standing.Section, standing.Section, pricing, currency.Value);

            if (line.IsFailure)
            {
                return line.Error;
            }

            lines.AddRange(Enumerable.Repeat(line.Value, standing.Quantity));
        }

        return lines;
    }

    private static Result<OrderLine> LineFor(string ticket, string section, EventPricing pricing, CurrencyCode currency)
    {
        if (!pricing.PriceBySection.TryGetValue(section, out long amountMinor))
        {
            return OrderingApplicationErrors.UnpricedSection(section);
        }

        Result<Money> price = Money.Create(amountMinor, currency);

        if (price.IsFailure)
        {
            return price.Error;
        }

        return OrderLine.Create(ticket, price.Value);
    }
}
