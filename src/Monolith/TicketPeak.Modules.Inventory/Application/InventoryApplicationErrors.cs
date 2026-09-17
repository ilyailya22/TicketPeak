using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Application;

internal static class InventoryApplicationErrors
{
    public static Error InventoryNotOpened { get; } =
        Error.NotFound("Inventory.Event.NotOpened", "Tickets for this event have not been released for sale.");

    public static Error AlreadyOpened { get; } =
        Error.Conflict("Inventory.Event.AlreadyOpened", "Inventory for this event is already open.");

    public static Error EventNotPublished { get; } =
        Error.Failure("Inventory.Event.NotPublished", "Only a published event's inventory can be opened.");

    public static Error NotOnSale { get; } =
        Error.Failure("Inventory.Event.NotOnSale", "The event is not on sale right now.");

    public static Error HoldNotActive { get; } =
        Error.Failure("Inventory.Hold.NotActive", "The hold has expired, been released or already been used.");
}
