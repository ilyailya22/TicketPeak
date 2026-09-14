using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>Numbered seats, e.g. row C seat 14. Inventory sells each one exactly once.</summary>
internal sealed class ReservedSection : Section
{
    private ReservedSection(SectionCode code, IReadOnlyList<Row> rows)
        : base(code) => Rows = rows;

    public IReadOnlyList<Row> Rows { get; }

    public override int Capacity => Rows.Sum(row => row.SeatCount);

    public static Result<ReservedSection> Create(SectionCode code, IEnumerable<Row> rows)
    {
        Row[] rowList = [.. rows];

        if (rowList.Length == 0)
        {
            return CatalogErrors.SectionHasNoRows;
        }

        string? duplicate = rowList
            .GroupBy(row => row.Label, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();

        if (duplicate is not null)
        {
            return CatalogErrors.DuplicateRow(duplicate);
        }

        return new ReservedSection(code, Array.AsReadOnly(rowList));
    }
}
