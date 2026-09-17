using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

internal sealed record OrderCancelled(OrderId OrderId) : IDomainEvent;
