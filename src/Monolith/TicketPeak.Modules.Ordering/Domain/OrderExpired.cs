using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

internal sealed record OrderExpired(OrderId OrderId) : IDomainEvent;
