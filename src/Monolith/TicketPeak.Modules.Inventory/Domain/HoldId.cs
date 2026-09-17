using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Domain;

internal readonly record struct HoldId(Guid Value) : IStronglyTypedId;
