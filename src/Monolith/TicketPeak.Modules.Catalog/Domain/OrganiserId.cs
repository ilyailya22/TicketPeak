using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>
/// Catalog's own view of who owns an event. Identity owns organisers; Catalog keeps only the id,
/// so it never depends on another module's types.
/// </summary>
internal readonly record struct OrganiserId(Guid Value) : IStronglyTypedId;
