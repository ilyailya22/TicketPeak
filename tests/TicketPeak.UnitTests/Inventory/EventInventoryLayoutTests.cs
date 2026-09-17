using Microsoft.Extensions.Time.Testing;
using Shouldly;
using TicketPeak.Modules.Inventory.Domain;
using TicketPeak.Shared.Kernel;
using Xunit;
using static TicketPeak.UnitTests.Inventory.InventoryBuilders;

namespace TicketPeak.UnitTests.Inventory;

public sealed class EventInventoryLayoutTests
{
    private readonly EventId _eventId = new(Guid.NewGuid());

    [Fact]
    public void Create_WithNoSeatsAndNoStanding_IsRefused()
    {
        EventInventory.Create(_eventId, [], []).Error.ShouldBe(InventoryErrors.EmptyLayout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithARowOfFewerThanOneSeat_IsRefused(int seats)
    {
        EventInventory.Create(_eventId, [new ReservedRowLayout("STALLS", "A", seats)], []).Error
            .ShouldBe(InventoryErrors.InvalidSeatCount);
    }

    [Fact]
    public void Create_WithTheSameRowTwice_IsRefused()
    {
        Result<EventInventory> result = EventInventory.Create(
            _eventId,
            [new ReservedRowLayout("STALLS", "A", 10), new ReservedRowLayout("stalls", "a", 4)],
            []);

        result.Error.ShouldNotBeNull().Code.ShouldBe("Inventory.Layout.DuplicateRow");
    }

    [Fact]
    public void Create_WithASectionThatIsBothReservedAndStanding_IsRefused()
    {
        Result<EventInventory> result = EventInventory.Create(
            _eventId,
            [new ReservedRowLayout("FLOOR", "A", 10)],
            [new GeneralAdmissionLayout("FLOOR", 100)]);

        result.Error.ShouldNotBeNull().Code.ShouldBe("Inventory.Layout.DuplicateSection");
    }

    [Fact]
    public void Create_WithStandingCapacityBelowOne_IsRefused()
    {
        EventInventory.Create(_eventId, [], [new GeneralAdmissionLayout("FLOOR", 0)]).Error
            .ShouldBe(InventoryErrors.InvalidCapacity);
    }

    [Fact]
    public void Create_WithValidLayout_StartsWithEverythingAvailable()
    {
        FakeTimeProvider time = new(Now);

        EventInventory inventory = StandardInventory();

        inventory.SeatStatusOf(Seat("STALLS", "B", 12), time).Value.ShouldBe(SeatStatus.Available);
        inventory.AvailableCapacityOf("floor", time).Value.ShouldBe(10);
    }
}
