using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>Inventory's own view of an event: the id only, so it never depends on Catalog's types.</summary>
internal readonly record struct EventId(Guid Value) : IStronglyTypedId;
