using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>Identifies a section within a seat map, e.g. <c>STALLS</c> or <c>FLOOR</c>. Case-insensitive.</summary>
internal readonly record struct SectionCode
{
    private const int MaxLength = 20;

    private SectionCode(string value) => Value = value;

    public string Value { get; }

    public static Result<SectionCode> Create(string? value)
    {
        string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalized.Length is 0 or > MaxLength)
        {
            return CatalogErrors.InvalidSectionCode;
        }

        return new SectionCode(normalized);
    }

    public override string ToString() => Value;
}
