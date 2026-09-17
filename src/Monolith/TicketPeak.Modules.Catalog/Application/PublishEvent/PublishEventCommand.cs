using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.PublishEvent;

internal sealed record PublishEventCommand(Guid EventId) : IRequest<Result>, ICommand;
