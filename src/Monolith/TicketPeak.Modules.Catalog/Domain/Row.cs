using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>A row of numbered seats, 1 to <see cref="SeatCount"/>, in a reserved section.</summary>
internal readonly record struct Row
{
    private Row(string label, int seatCount)
    {
        Label = label;
        SeatCount = seatCount;
    }

    public string Label { get; }

    public int SeatCount { get; }

    public static Result<Row> Create(string? label, int seatCount)
    {
        string normalized = label?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalized.Length == 0)
        {
            return CatalogErrors.RowLabelRequired;
        }

        if (seatCount < 1)
        {
            return CatalogErrors.InvalidSeatCount;
        }

        return new Row(normalized, seatCount);
    }
}
