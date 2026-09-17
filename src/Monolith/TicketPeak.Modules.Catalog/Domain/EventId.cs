using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

internal readonly record struct EventId(Guid Value) : IStronglyTypedId;
