using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>A place where events happen, holding the seat map new events start from.</summary>
internal sealed class Venue : AggregateRoot<VenueId>
{
    private Venue(VenueId id, string name, SeatMap seatMap)
        : base(id)
    {
        Name = name;
        SeatMap = seatMap;
    }

    public string Name { get; }

    public SeatMap SeatMap { get; private set; }

    public static Result<Venue> Create(VenueId id, string? name, SeatMap seatMap)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return CatalogErrors.VenueNameRequired;
        }

        return new Venue(id, trimmed, seatMap);
    }

    /// <summary>Affects only events created from now on; existing events keep the seat map they were created with.</summary>
    public void ReplaceSeatMap(SeatMap seatMap) => SeatMap = seatMap;
}
