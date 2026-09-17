namespace TicketPeak.Modules.Catalog.Domain;

internal interface IVenueRepository
{
    Task<Venue?> FindAsync(VenueId id, CancellationToken cancellationToken);

    /// <summary>Stages a new venue; it is stored when the unit of work commits.</summary>
    void Add(Venue venue);
}
