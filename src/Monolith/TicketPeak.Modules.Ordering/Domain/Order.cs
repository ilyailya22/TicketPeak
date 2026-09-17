using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Domain;

/// <summary>
/// A buyer's purchase of held tickets. Its life is bounded by the hold it was placed against: once
/// that hold lapses the tickets may belong to someone else, so the order can no longer be paid.
/// </summary>
internal sealed class Order : AggregateRoot<OrderId>
{
    public const int MaxTicketsPerOrder = 8;

    private readonly OrderLine[] _lines;

    private Order(
        OrderId id,
        CustomerId customerId,
        EventId eventId,
        HoldId holdId,
        OrderLine[] lines,
        Money total,
        DateTimeOffset placedAt,
        DateTimeOffset expiresAt)
        : base(id)
    {
        CustomerId = customerId;
        EventId = eventId;
        HoldId = holdId;
        _lines = lines;
        Total = total;
        PlacedAt = placedAt;
        ExpiresAt = expiresAt;
        Status = OrderStatus.Placed;
    }

    public CustomerId CustomerId { get; }

    public EventId EventId { get; }

    public HoldId HoldId { get; }

    public IReadOnlyList<OrderLine> Lines => Array.AsReadOnly(_lines);

    public Money Total { get; }

    public DateTimeOffset PlacedAt { get; }

    /// <summary>When the underlying hold lapses. Payment at or after this instant is refused.</summary>
    public DateTimeOffset ExpiresAt { get; }

    public OrderStatus Status { get; private set; }

    public static Result<Order> Place(
        OrderId id,
        CustomerId customerId,
        EventId eventId,
        HoldId holdId,
        IEnumerable<OrderLine> lines,
        DateTimeOffset holdExpiresAt,
        TimeProvider time)
    {
        OrderLine[] lineList = [.. lines];

        if (lineList.Length == 0)
        {
            return OrderingErrors.EmptyOrder;
        }

        if (lineList.Length > MaxTicketsPerOrder)
        {
            return OrderingErrors.TooManyTickets;
        }

        CurrencyCode currency = lineList[0].Price.Currency;

        if (lineList.Any(line => line.Price.Currency != currency))
        {
            return OrderingErrors.MixedCurrencies;
        }

        DateTimeOffset now = time.GetUtcNow();

        if (holdExpiresAt <= now)
        {
            return OrderingErrors.HoldAlreadyExpired;
        }

        // Cannot fail: every line's price is already a valid, non-negative Money in one currency.
        Money total = Money.Create(lineList.Sum(line => line.Price.AmountMinor), currency).Value;

        Order order = new(id, customerId, eventId, holdId, lineList, total, now, holdExpiresAt);
        order.Raise(new OrderPlaced(id, total));
        return order;
    }

    public Result MarkPaid(TimeProvider time)
    {
        if (Status != OrderStatus.Placed)
        {
            return OrderingErrors.NotAwaitingPayment(Status);
        }

        if (time.GetUtcNow() >= ExpiresAt)
        {
            return OrderingErrors.PaymentTooLate;
        }

        Status = OrderStatus.Paid;
        Raise(new OrderPaid(Id));
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status != OrderStatus.Paid)
        {
            return OrderingErrors.NotPaid;
        }

        Status = OrderStatus.Completed;
        Raise(new OrderCompleted(Id));
        return Result.Success();
    }

    /// <summary>Only while awaiting payment. Undoing a paid order is a refund, which is Phase 8's compensation.</summary>
    public Result Cancel()
    {
        if (Status != OrderStatus.Placed)
        {
            return OrderingErrors.CannotCancel;
        }

        Status = OrderStatus.Cancelled;
        Raise(new OrderCancelled(Id));
        return Result.Success();
    }

    public Result Expire(TimeProvider time)
    {
        if (Status != OrderStatus.Placed)
        {
            return OrderingErrors.NotAwaitingPayment(Status);
        }

        if (time.GetUtcNow() < ExpiresAt)
        {
            return OrderingErrors.NotYetExpired;
        }

        Status = OrderStatus.Expired;
        Raise(new OrderExpired(Id));
        return Result.Success();
    }
}
