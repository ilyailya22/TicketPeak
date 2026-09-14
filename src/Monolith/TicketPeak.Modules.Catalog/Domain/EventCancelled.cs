using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

internal sealed record EventCancelled(EventId EventId) : IDomainEvent;
