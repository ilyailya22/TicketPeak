using Shouldly;
using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;
using Xunit;
using static TicketPeak.UnitTests.Catalog.CatalogBuilders;

namespace TicketPeak.UnitTests.Catalog;

public sealed class SeatMapTests
{
    [Fact]
    public void Create_WithNoSections_IsRefused()
    {
        SeatMap.Create([]).Error.ShouldBe(CatalogErrors.SeatMapHasNoSections);
    }

    [Fact]
    public void Create_WithTwoSectionsSharingACode_IsRefused()
    {
        Result<SeatMap> result = SeatMap.Create([GeneralAdmissionOf("FLOOR", 100), GeneralAdmissionOf("floor", 50)]);

        result.Error.ShouldNotBeNull().Code.ShouldBe("Catalog.SeatMap.DuplicateSection");
    }

    [Fact]
    public void Capacity_OfMixedSeatMap_SumsReservedSeatsAndStandingPlaces()
    {
        StandardSeatMap().Capacity.ShouldBe(10 + 12 + 500);
    }

    [Fact]
    public void ReservedSection_WithNoRows_IsRefused()
    {
        ReservedSection.Create(CodeOf("STALLS"), []).Error.ShouldBe(CatalogErrors.SectionHasNoRows);
    }

    [Fact]
    public void ReservedSection_WithTwoRowsSharingALabel_IsRefused()
    {
        Result<ReservedSection> result = ReservedSection.Create(CodeOf("STALLS"), [RowOf("a", 10), RowOf("A", 12)]);

        result.Error.ShouldNotBeNull().Code.ShouldBe("Catalog.Section.DuplicateRow");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void GeneralAdmissionSection_WithFewerThanOnePlace_IsRefused(int capacity)
    {
        GeneralAdmissionSection.Create(CodeOf("FLOOR"), capacity).Error.ShouldBe(CatalogErrors.InvalidCapacity);
    }
}
