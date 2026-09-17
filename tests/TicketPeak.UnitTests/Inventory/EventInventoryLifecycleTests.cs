using Microsoft.Extensions.Time.Testing;
using Shouldly;
using TicketPeak.Modules.Inventory.Domain;
using Xunit;
using static TicketPeak.UnitTests.Inventory.InventoryBuilders;

namespace TicketPeak.UnitTests.Inventory;

/// <summary>Expiry, release and confirmation: how held tickets become free again or become sold.</summary>
public sealed class EventInventoryLifecycleTests
{
    private readonly FakeTimeProvider _time = new(Now);
    private readonly EventInventory _inventory = StandardInventory();

    [Fact]
    public void Seat_OneTickBeforeTheHoldExpires_IsStillHeld()
    {
        HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));

        _time.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromTicks(1));

        _inventory.SeatStatusOf(Seat("STALLS", "A", 1), _time).Value.ShouldBe(SeatStatus.Held);
    }

    [Fact]
    public void Seat_WhenTheHoldExpires_IsAvailableAgainWithNoJobRunning()
    {
        HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));

        _time.Advance(TimeSpan.FromMinutes(10));

        _inventory.SeatStatusOf(Seat("STALLS", "A", 1), _time).Value.ShouldBe(SeatStatus.Available);
    }

    [Fact]
    public void PlaceHold_OnASeatWhoseHoldExpired_IsAccepted()
    {
        HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));
        _time.Advance(TimeSpan.FromMinutes(10));

        _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "A", 1)], [], _time).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void StandingPlaces_WhenTheHoldExpires_AreReturnedToCapacity()
    {
        HoldStanding(_inventory, _time, 6);
        _inventory.AvailableCapacityOf("FLOOR", _time).Value.ShouldBe(4);

        _time.Advance(TimeSpan.FromMinutes(10));

        _inventory.AvailableCapacityOf("FLOOR", _time).Value.ShouldBe(10);
    }

    [Fact]
    public void Release_OfAnActiveHold_FreesItsTicketsImmediately()
    {
        Hold hold = _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "A", 1)], [Standing(3)], _time).Value;

        _inventory.Release(hold.Id).IsSuccess.ShouldBeTrue();

        _inventory.SeatStatusOf(Seat("STALLS", "A", 1), _time).Value.ShouldBe(SeatStatus.Available);
        _inventory.AvailableCapacityOf("FLOOR", _time).Value.ShouldBe(10);
    }

    [Fact]
    public void Release_CalledTwice_IsHarmless()
    {
        Hold hold = HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));
        _inventory.Release(hold.Id);

        _inventory.Release(hold.Id).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Release_OfAnExpiredHold_IsHarmless()
    {
        Hold hold = HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));
        _time.Advance(TimeSpan.FromHours(1));

        _inventory.Release(hold.Id).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Release_OfAConfirmedHold_IsRefused()
    {
        Hold hold = HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));
        _inventory.Confirm(hold.Id, _time);

        _inventory.Release(hold.Id).Error.ShouldBe(InventoryErrors.HoldAlreadyConfirmed);
        _inventory.SeatStatusOf(Seat("STALLS", "A", 1), _time).Value.ShouldBe(SeatStatus.Sold);
    }

    [Fact]
    public void Release_OfAnUnknownHold_IsRefused()
    {
        _inventory.Release(NewHoldId()).Error.ShouldBe(InventoryErrors.HoldNotFound);
    }

    [Fact]
    public void Confirm_OfAnActiveHold_SellsItsTicketsAndRaisesHoldConfirmed()
    {
        Hold hold = _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "B", 3)], [Standing(4)], _time).Value;

        _inventory.Confirm(hold.Id, _time).IsSuccess.ShouldBeTrue();

        _inventory.SeatStatusOf(Seat("STALLS", "B", 3), _time).Value.ShouldBe(SeatStatus.Sold);
        _inventory.AvailableCapacityOf("FLOOR", _time).Value.ShouldBe(6);
        HoldConfirmed raised = _inventory.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<HoldConfirmed>();
        raised.HoldId.ShouldBe(hold.Id);
        raised.EventId.ShouldBe(_inventory.Id);
    }

    [Fact]
    public void Confirm_AfterTheHoldExpired_IsRefusedAndSellsNothing()
    {
        Hold hold = HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));
        _time.Advance(TimeSpan.FromMinutes(10));

        _inventory.Confirm(hold.Id, _time).Error.ShouldBe(InventoryErrors.HoldExpired);

        _inventory.SeatStatusOf(Seat("STALLS", "A", 1), _time).Value.ShouldBe(SeatStatus.Available);
    }

    [Fact]
    public void Confirm_OfAReleasedHold_IsRefused()
    {
        Hold hold = HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));
        _inventory.Release(hold.Id);

        _inventory.Confirm(hold.Id, _time).Error.ShouldBe(InventoryErrors.HoldReleased);
    }

    [Fact]
    public void Confirm_CalledTwice_IsRefused()
    {
        Hold hold = HoldSeats(_inventory, _time, Seat("STALLS", "A", 1));
        _inventory.Confirm(hold.Id, _time);

        _inventory.Confirm(hold.Id, _time).Error.ShouldBe(InventoryErrors.HoldAlreadyConfirmed);
    }

    [Fact]
    public void SoldTickets_LongAfterTheHoldWouldHaveExpired_StaySold()
    {
        Hold hold = _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "A", 1)], [Standing(5)], _time).Value;
        _inventory.Confirm(hold.Id, _time);

        _time.Advance(TimeSpan.FromDays(1));

        _inventory.SeatStatusOf(Seat("STALLS", "A", 1), _time).Value.ShouldBe(SeatStatus.Sold);
        _inventory.AvailableCapacityOf("FLOOR", _time).Value.ShouldBe(5);
    }
}
