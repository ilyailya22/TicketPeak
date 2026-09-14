using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>
/// An amount in the currency's minor unit (cents, pence), so no price is ever a rounded
/// floating-point number.
/// </summary>
internal readonly record struct Money
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
            return CatalogErrors.NegativePrice;
        }

        return new Money(amountMinor, currency);
    }
}
