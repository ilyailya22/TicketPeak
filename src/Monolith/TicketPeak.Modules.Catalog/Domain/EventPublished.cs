using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

internal sealed record EventPublished(EventId EventId) : IDomainEvent;
