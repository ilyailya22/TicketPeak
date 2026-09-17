using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.CreateVenue;

/// <summary>Builds a domain seat map from request input, stopping at the first rule the domain refuses.</summary>
internal static class SeatMapFactory
{
    public static Result<SeatMap> FromInput(IEnumerable<SectionInput> sections)
    {
        List<Section> built = [];

        foreach (SectionInput input in sections)
        {
            Result<SectionCode> code = SectionCode.Create(input.Code);

            if (code.IsFailure)
            {
                return code.Error;
            }

            Result<Section> section = input.StandingCapacity is int capacity
                ? Widen(GeneralAdmissionSection.Create(code.Value, capacity))
                : BuildReserved(code.Value, input.Rows ?? []);

            if (section.IsFailure)
            {
                return section.Error;
            }

            built.Add(section.Value);
        }

        return SeatMap.Create(built);
    }

    private static Result<Section> BuildReserved(SectionCode code, IReadOnlyList<RowInput> rows)
    {
        List<Row> built = [];

        foreach (RowInput input in rows)
        {
            Result<Row> row = Row.Create(input.Label, input.Seats);

            if (row.IsFailure)
            {
                return row.Error;
            }

            built.Add(row.Value);
        }

        return Widen(ReservedSection.Create(code, built));
    }

    private static Result<Section> Widen<TSection>(Result<TSection> result)
        where TSection : Section =>
        result.IsSuccess ? result.Value : result.Error;
}
