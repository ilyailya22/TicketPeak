using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

/// <summary>Ordering's view of the buyer: the id only. Identity owns who they are.</summary>
internal readonly record struct CustomerId(Guid Value) : IStronglyTypedId;
