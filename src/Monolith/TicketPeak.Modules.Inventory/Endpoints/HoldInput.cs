using TicketPeak.Modules.Inventory.Application.PlaceHold;

namespace TicketPeak.Modules.Inventory.Endpoints;

/// <summary>A hold request body; the event id comes from the route. Either list may be omitted.</summary>
internal sealed record HoldInput(IReadOnlyList<SeatInput>? Seats, IReadOnlyList<StandingInput>? Standing);
