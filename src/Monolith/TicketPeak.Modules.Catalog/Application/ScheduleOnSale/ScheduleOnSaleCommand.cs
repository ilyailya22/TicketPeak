using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.ScheduleOnSale;

internal sealed record ScheduleOnSaleCommand(Guid EventId, DateTimeOffset OpensAt, DateTimeOffset ClosesAt)
    : IRequest<Result>, ICommand;
