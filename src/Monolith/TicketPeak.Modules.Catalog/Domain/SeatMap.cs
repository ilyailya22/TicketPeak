using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>
/// The sellable layout of a venue or event. Immutable, which is what makes "an event keeps the
/// seat map it was created with" true without copying: replacing a venue's seat map swaps the
/// venue's reference and cannot reach the instance an event already holds.
/// </summary>
internal sealed class SeatMap
{
    private SeatMap(IReadOnlyList<Section> sections) => Sections = sections;

    public IReadOnlyList<Section> Sections { get; }

    public int Capacity => Sections.Sum(section => section.Capacity);

    public bool HasSection(SectionCode code) => Sections.Any(section => section.Code == code);

    public static Result<SeatMap> Create(IEnumerable<Section> sections)
    {
        Section[] sectionList = [.. sections];

        if (sectionList.Length == 0)
        {
            return CatalogErrors.SeatMapHasNoSections;
        }

        IGrouping<SectionCode, Section>? duplicate = sectionList
            .GroupBy(section => section.Code)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            return CatalogErrors.DuplicateSection(duplicate.Key);
        }

        return new SeatMap(Array.AsReadOnly(sectionList));
    }
}
