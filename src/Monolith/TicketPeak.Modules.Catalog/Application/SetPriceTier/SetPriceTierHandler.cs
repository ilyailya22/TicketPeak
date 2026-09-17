using MediatR;
using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.SetPriceTier;

internal sealed class SetPriceTierHandler(IEventRepository events) : IRequestHandler<SetPriceTierCommand, Result>
{
    public async Task<Result> Handle(SetPriceTierCommand request, CancellationToken cancellationToken)
    {
        Event? catalogEvent = await events.FindAsync(new EventId(request.EventId), cancellationToken);

        if (catalogEvent is null)
        {
            return CatalogApplicationErrors.EventNotFound;
        }

        Result<SectionCode> section = SectionCode.Create(request.Section);

        if (section.IsFailure)
        {
            return section.Error;
        }

        Result<CurrencyCode> currency = CurrencyCode.Create(request.Currency);

        if (currency.IsFailure)
        {
            return currency.Error;
        }

        Result<Money> price = Money.Create(request.AmountMinor, currency.Value);

        if (price.IsFailure)
        {
            return price.Error;
        }

        return catalogEvent.SetPriceTier(section.Value, price.Value);
    }
}
