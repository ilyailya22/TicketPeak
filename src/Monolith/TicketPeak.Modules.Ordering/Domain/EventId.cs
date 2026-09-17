using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

/// <summary>Ordering's own view of an event: the id only, so it never depends on Catalog's types.</summary>
internal readonly record struct EventId(Guid Value) : IStronglyTypedId;
