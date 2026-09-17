using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

internal sealed record OrderPaid(OrderId OrderId) : IDomainEvent;
