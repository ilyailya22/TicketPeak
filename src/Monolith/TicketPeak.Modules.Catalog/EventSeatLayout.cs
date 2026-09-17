namespace TicketPeak.Modules.Catalog;

/// <summary>What an event sells, flattened for other modules: every reserved row and every standing area.</summary>
public sealed record EventSeatLayout(
    Guid EventId,
    bool IsPublished,
    IReadOnlyList<SeatRowLayout> ReservedRows,
    IReadOnlyList<StandingAreaLayout> StandingAreas);
