using System.Globalization;

namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>One numbered seat, e.g. STALLS row C seat 14. Section and row are case-insensitive.</summary>
internal readonly record struct SeatLocation
{
    private SeatLocation(string section, string row, int number)
    {
        Section = section;
        Row = row;
        Number = number;
    }

    public string Section { get; }

    public string Row { get; }

    public int Number { get; }

    public static SeatLocation Of(string section, string row, int number) =>
        new(Normalize(section), Normalize(row), number);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Section} {Row}-{Number}");

    internal static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
