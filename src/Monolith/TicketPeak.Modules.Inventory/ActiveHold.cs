namespace TicketPeak.Modules.Inventory;

/// <summary>A live hold: the tickets set aside and the instant they stop being set aside.</summary>
public sealed record ActiveHold(
    Guid EventId,
    Guid HoldId,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<HeldSeat> Seats,
    IReadOnlyList<HeldStandingPlaces> Standing);
