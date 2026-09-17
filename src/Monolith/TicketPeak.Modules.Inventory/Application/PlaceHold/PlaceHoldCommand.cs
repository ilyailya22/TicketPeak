using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Application.PlaceHold;

internal sealed record PlaceHoldCommand(
    Guid EventId,
    IReadOnlyList<SeatInput> Seats,
    IReadOnlyList<StandingInput> Standing)
    : IRequest<Result<HoldResponse>>, ICommand;

internal sealed record SeatInput(string Section, string Row, int Number);

internal sealed record StandingInput(string Section, int Quantity);

internal sealed record HoldResponse(Guid HoldId, DateTimeOffset ExpiresAt);
