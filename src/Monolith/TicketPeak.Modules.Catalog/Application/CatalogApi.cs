using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application;

internal sealed class CatalogApi(IEventRepository events, TimeProvider time) : ICatalogApi
{
    public async Task<Result<EventSeatLayout>> GetSeatLayoutAsync(Guid eventId, CancellationToken cancellationToken)
    {
        Event? catalogEvent = await events.FindAsync(new EventId(eventId), cancellationToken);

        if (catalogEvent is null)
        {
            return CatalogApplicationErrors.EventNotFound;
        }

        List<SeatRowLayout> rows = [];
        List<StandingAreaLayout> standingAreas = [];

        foreach (Section section in catalogEvent.SeatMap.Sections)
        {
            if (section is ReservedSection reserved)
            {
                rows.AddRange(reserved.Rows.Select(row => new SeatRowLayout(reserved.Code.Value, row.Label, row.SeatCount)));
            }
            else
            {
                standingAreas.Add(new StandingAreaLayout(section.Code.Value, section.Capacity));
            }
        }

        return new EventSeatLayout(eventId, catalogEvent.Status == EventStatus.Published, rows, standingAreas);
    }

    public async Task<Result<EventPricing>> GetPricingAsync(Guid eventId, CancellationToken cancellationToken)
    {
        Event? catalogEvent = await events.FindAsync(new EventId(eventId), cancellationToken);

        if (catalogEvent is null)
        {
            return CatalogApplicationErrors.EventNotFound;
        }

        var priceBySection = catalogEvent.PriceTiers.ToDictionary(
            tier => tier.Key.Value,
            tier => tier.Value.AmountMinor,
            StringComparer.Ordinal);

        return new EventPricing(eventId, catalogEvent.Currency.Value, priceBySection);
    }

    public async Task<Result<bool>> IsOnSaleAsync(Guid eventId, CancellationToken cancellationToken)
    {
        Event? catalogEvent = await events.FindAsync(new EventId(eventId), cancellationToken);

        if (catalogEvent is null)
        {
            return CatalogApplicationErrors.EventNotFound;
        }

        return catalogEvent.IsOnSale(time);
    }
}
