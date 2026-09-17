using Shouldly;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.UnitTests.SharedKernel;

public sealed class MoneyTests
{
    private static readonly CurrencyCode _euro = CurrencyCode.Create("EUR").Value;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void CurrencyCode_WhenNotThreeLetters_IsRefused(string? value)
    {
        CurrencyCode.Create(value).Error.ShouldBe(MoneyErrors.InvalidCurrency);
    }

    [Fact]
    public void CurrencyCode_WhenLowercase_IsNormalised()
    {
        CurrencyCode.Create("eur").Value.Value.ShouldBe("EUR");
    }

    [Fact]
    public void Money_WhenNegative_IsRefused()
    {
        Money.Create(-1, _euro).Error.ShouldBe(MoneyErrors.Negative);
    }

    [Fact]
    public void Money_WhenZero_IsAllowed()
    {
        Money.Create(0, _euro).IsSuccess.ShouldBeTrue();
    }
}
