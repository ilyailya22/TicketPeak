using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Payments.Domain;

internal sealed record PaymentFailed(PaymentIntentId PaymentIntentId, OrderId OrderId, string Reason) : IDomainEvent;
