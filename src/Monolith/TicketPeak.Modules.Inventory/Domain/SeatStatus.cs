namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>A reserved seat is in exactly one of these states at any instant.</summary>
internal enum SeatStatus
{
    Available = 0,
    Held = 1,
    Sold = 2,
}
