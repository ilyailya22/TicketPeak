using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.UnitTests.Catalog;

/// <summary>Valid building blocks, so each test spells out only the detail it is about.</summary>
internal static class CatalogBuilders
{
    public static DateTimeOffset Now { get; } = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    public static SectionCode CodeOf(string value) => SectionCode.Create(value).Value;

    public static Money PriceOf(long amountMinor, string currency = "EUR") =>
        Money.Create(amountMinor, CurrencyCode.Create(currency).Value).Value;

    public static Row RowOf(string label, int seats) => Row.Create(label, seats).Value;

    public static ReservedSection ReservedSectionOf(string code, params Row[] rows) =>
        ReservedSection.Create(CodeOf(code), rows).Value;

    public static GeneralAdmissionSection GeneralAdmissionOf(string code, int capacity) =>
        GeneralAdmissionSection.Create(CodeOf(code), capacity).Value;

    public static SeatMap SeatMapOf(params Section[] sections) => SeatMap.Create(sections).Value;

    /// <summary>STALLS: reserved rows A (10 seats) and B (12). FLOOR: 500 standing. 522 in total.</summary>
    public static SeatMap StandardSeatMap() =>
        SeatMapOf(
            ReservedSectionOf("STALLS", RowOf("A", 10), RowOf("B", 12)),
            GeneralAdmissionOf("FLOOR", 500));

    public static Venue VenueWith(SeatMap seatMap) =>
        Venue.Create(new VenueId(Guid.NewGuid()), "Roundhouse", seatMap).Value;

    public static OnSaleWindow WindowOf(DateTimeOffset opensAt, DateTimeOffset closesAt) =>
        OnSaleWindow.Create(opensAt, closesAt).Value;

    /// <summary>A draft starting 60 days after <see cref="Now"/>, priced in EUR.</summary>
    public static Event DraftEventAt(Venue venue) =>
        Event.Create(
            new EventId(Guid.NewGuid()),
            new OrganiserId(Guid.NewGuid()),
            venue,
            "Autumn Tour",
            Now.AddDays(60),
            CurrencyCode.Create("EUR").Value).Value;

    public static Event DraftEvent() => DraftEventAt(VenueWith(StandardSeatMap()));

    /// <summary>A draft with every section priced and sales open from day 1 to day 30 after <see cref="Now"/>.</summary>
    public static Event PublishableEvent()
    {
        Event concert = DraftEvent();
        concert.SetPriceTier(CodeOf("STALLS"), PriceOf(4500));
        concert.SetPriceTier(CodeOf("FLOOR"), PriceOf(3000));
        concert.ScheduleOnSale(WindowOf(Now.AddDays(1), Now.AddDays(30)));
        return concert;
    }
}
