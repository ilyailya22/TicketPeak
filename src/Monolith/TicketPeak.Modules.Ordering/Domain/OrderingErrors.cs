using System.Globalization;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

/// <summary>Every expected failure in the Ordering domain. Codes are stable; messages are for humans.</summary>
internal static class OrderingErrors
{
    public static Error TicketRequired { get; } =
        Error.Validation("Ordering.Line.TicketRequired", "An order line must describe its ticket.");

    public static Error EmptyOrder { get; } =
        Error.Validation("Ordering.Order.Empty", "An order must include at least one ticket.");

    public static Error TooManyTickets { get; } =
        Error.Validation(
            "Ordering.Order.TooManyTickets",
            string.Create(CultureInfo.InvariantCulture, $"An order can include at most {Order.MaxTicketsPerOrder} tickets."));

    public static Error MixedCurrencies { get; } =
        Error.Validation("Ordering.Order.MixedCurrencies", "Every ticket in an order must be priced in the same currency.");

    public static Error HoldAlreadyExpired { get; } =
        Error.Failure("Ordering.Order.HoldExpired", "The hold has expired, so its tickets can no longer be ordered.");

    public static Error PaymentTooLate { get; } =
        Error.Failure("Ordering.Order.PaymentTooLate", "Payment arrived after the order expired; its tickets may already belong to someone else.");

    public static Error NotPaid { get; } =
        Error.Failure("Ordering.Order.NotPaid", "Only a paid order can be completed.");

    public static Error CannotCancel { get; } =
        Error.Failure("Ordering.Order.CannotCancel", "Only an order awaiting payment can be cancelled; a paid order is refunded instead.");

    public static Error NotYetExpired { get; } =
        Error.Failure("Ordering.Order.NotYetExpired", "An order cannot expire before its hold does.");

    public static Error NotAwaitingPayment(OrderStatus status) =>
        Error.Failure("Ordering.Order.NotAwaitingPayment", $"The order is {status}, not awaiting payment.");
}
