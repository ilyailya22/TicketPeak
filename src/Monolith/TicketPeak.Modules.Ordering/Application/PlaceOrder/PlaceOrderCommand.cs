using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Application.PlaceOrder;

internal sealed record PlaceOrderCommand(Guid CustomerId, Guid EventId, Guid HoldId) : IRequest<Result<Guid>>, ICommand;
