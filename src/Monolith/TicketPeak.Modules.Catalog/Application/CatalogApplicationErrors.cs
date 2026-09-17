using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application;

internal static class CatalogApplicationErrors
{
    public static Error VenueNotFound { get; } =
        Error.NotFound("Catalog.Venue.NotFound", "No venue with this id exists.");

    public static Error EventNotFound { get; } =
        Error.NotFound("Catalog.Event.NotFound", "No event with this id exists.");
}
