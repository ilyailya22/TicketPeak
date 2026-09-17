using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog;

/// <summary>
/// The only way another module may reach Catalog. Reads only, called synchronously from inside the
/// checkout's consistency boundary; nothing outside Catalog changes Catalog's state. See ADR 0004.
/// </summary>
public interface ICatalogApi
{
    Task<Result<EventSeatLayout>> GetSeatLayoutAsync(Guid eventId, CancellationToken cancellationToken);

    Task<Result<EventPricing>> GetPricingAsync(Guid eventId, CancellationToken cancellationToken);

    Task<Result<bool>> IsOnSaleAsync(Guid eventId, CancellationToken cancellationToken);
}
