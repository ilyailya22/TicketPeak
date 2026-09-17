using MediatR;
using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.CreateEvent;

internal sealed class CreateEventHandler(IVenueRepository venues, IEventRepository events, TimeProvider time)
    : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        Venue? venue = await venues.FindAsync(new VenueId(request.VenueId), cancellationToken);

        if (venue is null)
        {
            return CatalogApplicationErrors.VenueNotFound;
        }

        Result<CurrencyCode> currency = CurrencyCode.Create(request.Currency);

        if (currency.IsFailure)
        {
            return currency.Error;
        }

        EventId id = new(Guid.CreateVersion7(time.GetUtcNow()));
        Result<Event> created = Event.Create(
            id,
            new OrganiserId(request.OrganiserId),
            venue,
            request.Title,
            request.StartsAt,
            currency.Value);

        if (created.IsFailure)
        {
            return created.Error;
        }

        events.Add(created.Value);
        return id.Value;
    }
}
