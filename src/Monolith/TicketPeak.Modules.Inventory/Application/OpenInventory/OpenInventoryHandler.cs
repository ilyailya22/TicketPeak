using MediatR;
using TicketPeak.Modules.Catalog;
using TicketPeak.Modules.Inventory.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Application.OpenInventory;

internal sealed class OpenInventoryHandler(ICatalogApi catalog, IEventInventoryRepository inventories)
    : IRequestHandler<OpenInventoryCommand, Result>
{
    public async Task<Result> Handle(OpenInventoryCommand request, CancellationToken cancellationToken)
    {
        EventId id = new(request.EventId);

        if (await inventories.FindAsync(id, cancellationToken) is not null)
        {
            return InventoryApplicationErrors.AlreadyOpened;
        }

        Result<EventSeatLayout> layout = await catalog.GetSeatLayoutAsync(request.EventId, cancellationToken);

        if (layout.IsFailure)
        {
            return layout.Error;
        }

        if (!layout.Value.IsPublished)
        {
            return InventoryApplicationErrors.EventNotPublished;
        }

        Result<EventInventory> inventory = EventInventory.Create(
            id,
            layout.Value.ReservedRows.Select(row => new ReservedRowLayout(row.Section, row.Row, row.SeatCount)),
            layout.Value.StandingAreas.Select(area => new GeneralAdmissionLayout(area.Section, area.Capacity)));

        if (inventory.IsFailure)
        {
            return inventory.Error;
        }

        inventories.Add(inventory.Value);
        return Result.Success();
    }
}
