using TicketPeak.Modules.Inventory.Domain;

namespace TicketPeak.UnitTests.Inventory;

/// <summary>Valid building blocks, so each test spells out only the detail it is about.</summary>
internal static class InventoryBuilders
{
    public static DateTimeOffset Now { get; } = new(2026, 10, 1, 19, 0, 0, TimeSpan.Zero);

    public static SeatLocation Seat(string section, string row, int number) => SeatLocation.Of(section, row, number);

    public static GeneralAdmissionQuantity Standing(int quantity, string section = "FLOOR") => new(section, quantity);

    public static HoldId NewHoldId() => new(Guid.NewGuid());

    /// <summary>STALLS: reserved rows A (10 seats) and B (12). FLOOR: 10 standing, small enough to fill in a test.</summary>
    public static EventInventory StandardInventory() =>
        EventInventory.Create(
            new EventId(Guid.NewGuid()),
            [new ReservedRowLayout("STALLS", "A", 10), new ReservedRowLayout("STALLS", "B", 12)],
            [new GeneralAdmissionLayout("FLOOR", 10)]).Value;

    public static Hold HoldSeats(EventInventory inventory, TimeProvider time, params SeatLocation[] seats) =>
        inventory.PlaceHold(NewHoldId(), seats, [], time).Value;

    public static Hold HoldStanding(EventInventory inventory, TimeProvider time, int quantity) =>
        inventory.PlaceHold(NewHoldId(), [], [Standing(quantity)], time).Value;
}
