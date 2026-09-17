using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Application;

internal static class OrderingApplicationErrors
{
    public static Error OrderNotFound { get; } =
        Error.NotFound("Ordering.Order.NotFound", "No order with this id exists.");

    public static Error HoldAlreadyOrdered { get; } =
        Error.Conflict("Ordering.Order.HoldAlreadyOrdered", "An order has already been placed for this hold.");

    public static Error UnpricedSection(string section) =>
        Error.Failure("Ordering.Order.UnpricedSection", $"Section '{section}' has no price, so its tickets cannot be ordered.");
}
