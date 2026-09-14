using Shouldly;
using TicketPeak.Modules.Catalog.Domain;
using Xunit;
using static TicketPeak.UnitTests.Catalog.CatalogBuilders;

namespace TicketPeak.UnitTests.Catalog;

public sealed class VenueTests
{
    [Fact]
    public void Create_WithBlankName_IsRefused()
    {
        Venue.Create(new VenueId(Guid.NewGuid()), "  ", StandardSeatMap()).Error
            .ShouldBe(CatalogErrors.VenueNameRequired);
    }

    [Fact]
    public void ReplaceSeatMap_AfterAnEventWasCreated_LeavesTheEventsSeatMapUnchanged()
    {
        SeatMap original = StandardSeatMap();
        Venue venue = VenueWith(original);
        Event concert = DraftEventAt(venue);

        venue.ReplaceSeatMap(SeatMapOf(GeneralAdmissionOf("FLOOR", 2000)));

        concert.SeatMap.ShouldBeSameAs(original);
        venue.SeatMap.ShouldNotBeSameAs(original);
    }
}
