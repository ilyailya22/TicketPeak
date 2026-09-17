using TicketPeak.Modules.Inventory.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Application;

internal sealed class InventoryApi(IEventInventoryRepository inventories, TimeProvider time) : IInventoryApi
{
    public async Task<Result<ActiveHold>> GetActiveHoldAsync(Guid eventId, Guid holdId, CancellationToken cancellationToken)
    {
        EventInventory? inventory = await inventories.FindAsync(new EventId(eventId), cancellationToken);

        if (inventory is null)
        {
            return InventoryApplicationErrors.InventoryNotOpened;
        }

        Result<Hold> hold = inventory.FindHold(new HoldId(holdId));

        if (hold.IsFailure)
        {
            return hold.Error;
        }

        if (!hold.Value.IsActiveAt(time.GetUtcNow()))
        {
            return InventoryApplicationErrors.HoldNotActive;
        }

        return new ActiveHold(
            eventId,
            holdId,
            hold.Value.ExpiresAt,
            [.. hold.Value.Seats.Select(seat => new HeldSeat(seat.Section, seat.Row, seat.Number))],
            [.. hold.Value.GeneralAdmission.Select(standing => new HeldStandingPlaces(standing.Key, standing.Value))]);
    }
}
