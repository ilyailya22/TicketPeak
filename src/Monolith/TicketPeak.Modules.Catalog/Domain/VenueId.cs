using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

internal readonly record struct VenueId(Guid Value) : IStronglyTypedId;
