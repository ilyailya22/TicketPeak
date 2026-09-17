using Microsoft.Extensions.Time.Testing;
using Shouldly;
using TicketPeak.Modules.Inventory.Domain;
using TicketPeak.Shared.Kernel;
using Xunit;
using static TicketPeak.UnitTests.Inventory.InventoryBuilders;

namespace TicketPeak.UnitTests.Inventory;

public sealed class EventInventoryHoldTests
{
    private readonly FakeTimeProvider _time = new(Now);
    private readonly EventInventory _inventory = StandardInventory();

    [Fact]
    public void PlaceHold_OnAvailableSeats_HoldsThemForTenMinutes()
    {
        Hold hold = _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "A", 1), Seat("STALLS", "A", 2)], [], _time).Value;

        hold.ExpiresAt.ShouldBe(Now.AddMinutes(10));
        _inventory.SeatStatusOf(Seat("STALLS", "A", 1), _time).Value.ShouldBe(SeatStatus.Held);
        _inventory.SeatStatusOf(Seat("STALLS", "A", 3), _time).Value.ShouldBe(SeatStatus.Available);
    }

    [Fact]
    public void PlaceHold_OnASeatSomeoneElseHolds_IsRefusedAsAConflict()
    {
        HoldSeats(_inventory, _time, Seat("STALLS", "A", 10));

        Error error = _inventory.PlaceHold(NewHoldId(), [Seat("stalls", "a", 10)], [], _time).Error.ShouldNotBeNull();

        error.Code.ShouldBe("Inventory.Seat.Unavailable");
        error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public void PlaceHold_OnASoldSeat_IsRefused()
    {
        Hold first = HoldSeats(_inventory, _time, Seat("STALLS", "B", 5));
        _inventory.Confirm(first.Id, _time);

        _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "B", 5)], [], _time).Error.ShouldNotBeNull()
            .Code.ShouldBe("Inventory.Seat.Unavailable");
    }

    [Fact]
    public void PlaceHold_WhenOneOfSeveralSeatsIsTaken_HoldsNothing()
    {
        HoldSeats(_inventory, _time, Seat("STALLS", "A", 2));

        _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "A", 1), Seat("STALLS", "A", 2)], [], _time).IsFailure.ShouldBeTrue();

        _inventory.SeatStatusOf(Seat("STALLS", "A", 1), _time).Value.ShouldBe(SeatStatus.Available);
    }

    [Fact]
    public void PlaceHold_OnASeatThatDoesNotExist_IsRefused()
    {
        _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "A", 11)], [], _time).Error.ShouldNotBeNull()
            .Code.ShouldBe("Inventory.Seat.Unknown");
    }

    [Fact]
    public void PlaceHold_RequestingTheSameSeatTwice_IsRefused()
    {
        _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "A", 1), Seat("stalls", "a", 1)], [], _time).Error.ShouldNotBeNull()
            .Code.ShouldBe("Inventory.Hold.DuplicateSeat");
    }

    [Fact]
    public void PlaceHold_WithNoTickets_IsRefused()
    {
        _inventory.PlaceHold(NewHoldId(), [], [], _time).Error.ShouldBe(InventoryErrors.EmptyHold);
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(9, false)]
    public void PlaceHold_AllowsAtMostEightTickets(int seats, bool accepted)
    {
        SeatLocation[] requested = [.. Enumerable.Range(1, seats).Select(number => Seat("STALLS", "B", number))];

        Result<Hold> result = _inventory.PlaceHold(NewHoldId(), requested, [], _time);

        result.IsSuccess.ShouldBe(accepted);
        if (!accepted)
        {
            result.Error.ShouldBe(InventoryErrors.TooManyTickets);
        }
    }

    [Fact]
    public void PlaceHold_CountsSeatsAndStandingTowardsTheSameLimit()
    {
        _inventory.PlaceHold(NewHoldId(), [Seat("STALLS", "A", 1), Seat("STALLS", "A", 2)], [Standing(7)], _time).Error
            .ShouldBe(InventoryErrors.TooManyTickets);
    }

    [Fact]
    public void PlaceHold_WithTheSameHoldIdTwice_IsRefused()
    {
        HoldId holdId = NewHoldId();
        _inventory.PlaceHold(holdId, [Seat("STALLS", "A", 1)], [], _time);

        _inventory.PlaceHold(holdId, [Seat("STALLS", "A", 2)], [], _time).Error.ShouldBe(InventoryErrors.DuplicateHold);
    }

    [Fact]
    public void PlaceHold_WithAStandingQuantityBelowOne_IsRefused()
    {
        _inventory.PlaceHold(NewHoldId(), [], [Standing(0)], _time).Error.ShouldBe(InventoryErrors.InvalidQuantity);
    }

    [Fact]
    public void PlaceHold_InAStandingSectionThatDoesNotExist_IsRefused()
    {
        _inventory.PlaceHold(NewHoldId(), [], [Standing(1, "BALCONY")], _time).Error.ShouldNotBeNull()
            .Code.ShouldBe("Inventory.Section.Unknown");
    }

    [Fact]
    public void PlaceHold_ForMoreStandingPlacesThanRemain_IsRefusedAsAConflict()
    {
        HoldStanding(_inventory, _time, 8);

        Error error = _inventory.PlaceHold(NewHoldId(), [], [Standing(3)], _time).Error.ShouldNotBeNull();

        error.Code.ShouldBe("Inventory.Section.InsufficientCapacity");
        error.Type.ShouldBe(ErrorType.Conflict);
        error.Message.ShouldContain("Only 2");
    }

    [Fact]
    public void PlaceHold_ForExactlyTheRemainingStandingPlaces_IsAccepted()
    {
        HoldStanding(_inventory, _time, 8);

        _inventory.PlaceHold(NewHoldId(), [], [Standing(2)], _time).IsSuccess.ShouldBeTrue();
        _inventory.AvailableCapacityOf("FLOOR", _time).Value.ShouldBe(0);
    }

    [Fact]
    public void PlaceHold_CountsSoldAndHeldTogetherAgainstStandingCapacity()
    {
        Hold sold = HoldStanding(_inventory, _time, 8);
        _inventory.Confirm(sold.Id, _time);
        HoldStanding(_inventory, _time, 2);

        _inventory.PlaceHold(NewHoldId(), [], [Standing(1)], _time).Error.ShouldNotBeNull()
            .Code.ShouldBe("Inventory.Section.InsufficientCapacity");
    }
}
