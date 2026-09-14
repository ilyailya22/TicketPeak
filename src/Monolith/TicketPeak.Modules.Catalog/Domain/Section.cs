namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>A part of a seat map sold as one kind of inventory: numbered seats or a standing capacity.</summary>
internal abstract class Section
{
    protected Section(SectionCode code) => Code = code;

    public SectionCode Code { get; }

    /// <summary>The most tickets this section can ever sell.</summary>
    public abstract int Capacity { get; }
}
