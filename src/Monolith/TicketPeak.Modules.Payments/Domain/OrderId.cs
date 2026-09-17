using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Payments.Domain;

/// <summary>Payments' own view of the order being paid for: the id only.</summary>
internal readonly record struct OrderId(Guid Value) : IStronglyTypedId;
