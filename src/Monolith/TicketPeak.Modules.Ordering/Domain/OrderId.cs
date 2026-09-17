using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

internal readonly record struct OrderId(Guid Value) : IStronglyTypedId;
