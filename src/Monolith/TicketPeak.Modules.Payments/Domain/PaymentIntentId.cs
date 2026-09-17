using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Payments.Domain;

internal readonly record struct PaymentIntentId(Guid Value) : IStronglyTypedId;
