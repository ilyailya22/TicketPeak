using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Payments.Domain;

internal sealed record PaymentAuthorized(PaymentIntentId PaymentIntentId, OrderId OrderId, Money Amount) : IDomainEvent;
