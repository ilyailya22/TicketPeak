using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

internal sealed record OrderPlaced(OrderId OrderId, Money Total) : IDomainEvent;
