using MediatR;
using TicketPeak.Modules.Catalog;
using TicketPeak.Modules.Inventory.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Application.PlaceHold;

internal sealed class PlaceHoldHandler(ICatalogApi catalog, IEventInventoryRepository inventories, TimeProvider time)
    : IRequestHandler<PlaceHoldCommand, Result<HoldResponse>>
{
    public async Task<Result<HoldResponse>> Handle(PlaceHoldCommand request, CancellationToken cancellationToken)
    {
        Result<bool> onSale = await catalog.IsOnSaleAsync(request.EventId, cancellationToken);

        if (onSale.IsFailure)
        {
            return onSale.Error;
        }

        if (!onSale.Value)
        {
            return InventoryApplicationErrors.NotOnSale;
        }

        EventInventory? inventory = await inventories.FindAsync(new EventId(request.EventId), cancellationToken);

        if (inventory is null)
        {
            return InventoryApplicationErrors.InventoryNotOpened;
        }

        HoldId holdId = new(Guid.CreateVersion7(time.GetUtcNow()));
        Result<Hold> hold = inventory.PlaceHold(
            holdId,
            [.. request.Seats.Select(seat => SeatLocation.Of(seat.Section, seat.Row, seat.Number))],
            [.. request.Standing.Select(standing => new GeneralAdmissionQuantity(standing.Section, standing.Quantity))],
            time);

        if (hold.IsFailure)
        {
            return hold.Error;
        }

        return new HoldResponse(holdId.Value, hold.Value.ExpiresAt);
    }
}
