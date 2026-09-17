using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Domain;

internal sealed record HoldConfirmed(EventId EventId, HoldId HoldId) : IDomainEvent;
