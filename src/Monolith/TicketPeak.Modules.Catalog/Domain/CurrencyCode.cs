using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>
/// A three-letter ISO 4217 code. Shape only: checking it against the live ISO list would put
/// reference data inside the domain for no invariant that needs it.
/// </summary>
internal readonly record struct CurrencyCode
{
    private CurrencyCode(string value) => Value = value;

    public string Value { get; }

    public static Result<CurrencyCode> Create(string? value)
    {
        string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetterUpper))
        {
            return CatalogErrors.InvalidCurrency;
        }

        return new CurrencyCode(normalized);
    }

    public override string ToString() => Value;
}
