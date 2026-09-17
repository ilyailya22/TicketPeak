using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.CreateEvent;

internal sealed record CreateEventCommand(
    Guid OrganiserId,
    Guid VenueId,
    string Title,
    DateTimeOffset StartsAt,
    string Currency)
    : IRequest<Result<Guid>>, ICommand;
