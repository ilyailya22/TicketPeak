using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

/// <summary>One ticket in an order, described as the buyer sees it, e.g. "STALLS A-14" or "FLOOR".</summary>
internal readonly record struct OrderLine
{
    private OrderLine(string ticket, Money price)
    {
        Ticket = ticket;
        Price = price;
    }

    public string Ticket { get; }

    public Money Price { get; }

    public static Result<OrderLine> Create(string? ticket, Money price)
    {
        string trimmed = ticket?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return OrderingErrors.TicketRequired;
        }

        return new OrderLine(trimmed, price);
    }
}
