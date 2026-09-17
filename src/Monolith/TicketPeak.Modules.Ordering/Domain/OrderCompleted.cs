using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

internal sealed record OrderCompleted(OrderId OrderId) : IDomainEvent;
