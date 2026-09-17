using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

/// <summary>Ordering's reference to the Inventory hold an order was placed against.</summary>
internal readonly record struct HoldId(Guid Value) : IStronglyTypedId;
