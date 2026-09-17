using Microsoft.Extensions.Time.Testing;
using Shouldly;
using TicketPeak.Modules.Ordering.Domain;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.UnitTests.Ordering;

public sealed class OrderTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 1, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _holdExpiresAt = _now.AddMinutes(10);

    private readonly FakeTimeProvider _time = new(_now);

    [Fact]
    public void Place_WithNoTickets_IsRefused()
    {
        Place([]).Error.ShouldBe(OrderingErrors.EmptyOrder);
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(9, false)]
    public void Place_AllowsAtMostEightTickets(int tickets, bool accepted)
    {
        Result<Order> result = Place(TicketsOf(tickets));

        result.IsSuccess.ShouldBe(accepted);
        if (!accepted)
        {
            result.Error.ShouldBe(OrderingErrors.TooManyTickets);
        }
    }

    [Fact]
    public void Place_WithTicketsInDifferentCurrencies_IsRefused()
    {
        Place([LineOf("STALLS A-1", 4500, "EUR"), LineOf("STALLS A-2", 4500, "GBP")]).Error
            .ShouldBe(OrderingErrors.MixedCurrencies);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Place_WhenTheHoldHasAlreadyExpired_IsRefused(int minutesUntilHoldExpires)
    {
        Place(TicketsOf(1), _now.AddMinutes(minutesUntilHoldExpires)).Error.ShouldBe(OrderingErrors.HoldAlreadyExpired);
    }

    [Fact]
    public void Place_WhenValid_TotalsTheTicketsExpiresWithTheHoldAndRaisesOrderPlaced()
    {
        Order order = Place([LineOf("STALLS A-1", 4500), LineOf("FLOOR", 3000)]).Value;

        order.Status.ShouldBe(OrderStatus.Placed);
        order.Total.AmountMinor.ShouldBe(7500);
        order.ExpiresAt.ShouldBe(_holdExpiresAt);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderPlaced>().Total.ShouldBe(order.Total);
    }

    [Fact]
    public void OrderLine_WithBlankTicket_IsRefused()
    {
        OrderLine.Create("  ", EurosOf(10)).Error.ShouldBe(OrderingErrors.TicketRequired);
    }

    [Fact]
    public void MarkPaid_BeforeTheOrderExpires_BecomesPaidAndRaisesOrderPaid()
    {
        Order order = PlacedOrder();
        _time.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromTicks(1));

        order.MarkPaid(_time).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Paid);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderPaid>();
    }

    [Fact]
    public void MarkPaid_OnceTheOrderHasExpired_IsRefused()
    {
        Order order = PlacedOrder();
        _time.Advance(TimeSpan.FromMinutes(10));

        order.MarkPaid(_time).Error.ShouldBe(OrderingErrors.PaymentTooLate);
        order.Status.ShouldBe(OrderStatus.Placed);
    }

    [Fact]
    public void MarkPaid_Twice_IsRefused()
    {
        Order order = PlacedOrder();
        order.MarkPaid(_time);

        order.MarkPaid(_time).Error.ShouldNotBeNull().Code.ShouldBe("Ordering.Order.NotAwaitingPayment");
    }

    [Fact]
    public void MarkPaid_AfterCancelling_IsRefused()
    {
        Order order = PlacedOrder();
        order.Cancel();

        order.MarkPaid(_time).Error.ShouldNotBeNull().Code.ShouldBe("Ordering.Order.NotAwaitingPayment");
    }

    [Fact]
    public void Complete_WhenPaid_BecomesCompletedAndRaisesOrderCompleted()
    {
        Order order = PlacedOrder();
        order.MarkPaid(_time);
        order.ClearDomainEvents();

        order.Complete().IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Completed);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderCompleted>();
    }

    [Fact]
    public void Complete_BeforePayment_IsRefused()
    {
        PlacedOrder().Complete().Error.ShouldBe(OrderingErrors.NotPaid);
    }

    [Fact]
    public void Cancel_WhileAwaitingPayment_BecomesCancelledAndRaisesOrderCancelled()
    {
        Order order = PlacedOrder();

        order.Cancel().IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderCancelled>();
    }

    [Fact]
    public void Cancel_AfterPayment_IsRefused()
    {
        Order order = PlacedOrder();
        order.MarkPaid(_time);

        order.Cancel().Error.ShouldBe(OrderingErrors.CannotCancel);
        order.Status.ShouldBe(OrderStatus.Paid);
    }

    [Fact]
    public void Expire_BeforeTheHoldLapses_IsRefused()
    {
        Order order = PlacedOrder();
        _time.Advance(TimeSpan.FromMinutes(9));

        order.Expire(_time).Error.ShouldBe(OrderingErrors.NotYetExpired);
    }

    [Fact]
    public void Expire_WhenTheHoldLapses_BecomesExpiredAndRaisesOrderExpired()
    {
        Order order = PlacedOrder();
        _time.Advance(TimeSpan.FromMinutes(10));

        order.Expire(_time).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Expired);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderExpired>();
    }

    [Fact]
    public void Expire_AfterPayment_IsRefused()
    {
        Order order = PlacedOrder();
        order.MarkPaid(_time);
        _time.Advance(TimeSpan.FromHours(1));

        order.Expire(_time).Error.ShouldNotBeNull().Code.ShouldBe("Ordering.Order.NotAwaitingPayment");
    }

    private static Money EurosOf(long amountMinor, string currency = "EUR") =>
        Money.Create(amountMinor, CurrencyCode.Create(currency).Value).Value;

    private static OrderLine LineOf(string ticket, long amountMinor, string currency = "EUR") =>
        OrderLine.Create(ticket, EurosOf(amountMinor, currency)).Value;

    private static OrderLine[] TicketsOf(int count) =>
        [.. Enumerable.Range(1, count).Select(number => LineOf($"STALLS B-{number}", 4500))];

    private Result<Order> Place(IEnumerable<OrderLine> lines, DateTimeOffset? holdExpiresAt = null) =>
        Order.Place(
            new OrderId(Guid.NewGuid()),
            new CustomerId(Guid.NewGuid()),
            new EventId(Guid.NewGuid()),
            new HoldId(Guid.NewGuid()),
            lines,
            holdExpiresAt ?? _holdExpiresAt,
            _time);

    /// <summary>A placed order with its OrderPlaced event already cleared, so tests see only what they caused.</summary>
    private Order PlacedOrder()
    {
        Order order = Place(TicketsOf(2)).Value;
        order.ClearDomainEvents();
        return order;
    }
}
