namespace TicketPeak.Shared.Kernel;

/// <summary>
/// An amount in the currency's minor unit (cents, pence), so no price is ever a rounded
/// floating-point number. Shared because Catalog prices, Ordering totals and Payments amounts must
/// mean exactly the same thing; three copies would be three chances to disagree.
/// </summary>
public readonly record struct Money
{
    private Money(long amountMinor, CurrencyCode currency)
    {
        AmountMinor = amountMinor;
        Currency = currency;
    }

    public long AmountMinor { get; }

    public CurrencyCode Currency { get; }

    public static Result<Money> Create(long amountMinor, CurrencyCode currency)
    {
        if (amountMinor < 0)
        {
            return MoneyErrors.Negative;
        }

        return new Money(amountMinor, currency);
    }
}
