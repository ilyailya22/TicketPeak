using Shouldly;
using TicketPeak.Modules.Catalog.Domain;
using Xunit;
using static TicketPeak.UnitTests.Catalog.CatalogBuilders;

namespace TicketPeak.UnitTests.Catalog;

public sealed class CatalogValueObjectTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void SectionCode_WhenBlankOrLongerThan20_IsRefused(string? value)
    {
        SectionCode.Create(value).Error.ShouldBe(CatalogErrors.InvalidSectionCode);
    }

    [Fact]
    public void SectionCode_WhenCreated_IsTrimmedAndUppercasedSoCaseDoesNotMatter()
    {
        SectionCode.Create("  stalls ").Value.ShouldBe(SectionCode.Create("STALLS").Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Row_WithFewerThanOneSeat_IsRefused(int seats)
    {
        Row.Create("A", seats).Error.ShouldBe(CatalogErrors.InvalidSeatCount);
    }

    [Fact]
    public void Row_WithBlankLabel_IsRefused()
    {
        Row.Create(" ", 10).Error.ShouldBe(CatalogErrors.RowLabelRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OnSaleWindow_WhenItDoesNotOpenBeforeItCloses_IsRefused(int closesAfterOpeningHours)
    {
        OnSaleWindow.Create(Now, Now.AddHours(closesAfterOpeningHours)).Error
            .ShouldBe(CatalogErrors.InvalidOnSaleWindow);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(10, false)]
    public void OnSaleWindow_Contains_IncludesOpeningButExcludesClosing(int secondsFromOpening, bool expected)
    {
        OnSaleWindow window = WindowOf(Now, Now.AddSeconds(10));

        window.Contains(Now.AddSeconds(secondsFromOpening)).ShouldBe(expected);
    }
}
