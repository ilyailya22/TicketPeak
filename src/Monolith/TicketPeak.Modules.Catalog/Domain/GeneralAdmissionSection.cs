using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>Unnumbered capacity, e.g. a standing floor. Inventory sells it as a count, not as places.</summary>
internal sealed class GeneralAdmissionSection : Section
{
    private GeneralAdmissionSection(SectionCode code, int capacity)
        : base(code) => Capacity = capacity;

    public override int Capacity { get; }

    public static Result<GeneralAdmissionSection> Create(SectionCode code, int capacity)
    {
        if (capacity < 1)
        {
            return CatalogErrors.InvalidCapacity;
        }

        return new GeneralAdmissionSection(code, capacity);
    }
}
