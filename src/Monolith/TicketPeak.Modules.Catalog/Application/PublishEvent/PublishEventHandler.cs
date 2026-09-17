using MediatR;
using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.PublishEvent;

internal sealed class PublishEventHandler(IEventRepository events, TimeProvider time)
    : IRequestHandler<PublishEventCommand, Result>
{
    public async Task<Result> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        Event? catalogEvent = await events.FindAsync(new EventId(request.EventId), cancellationToken);

        if (catalogEvent is null)
        {
            return CatalogApplicationErrors.EventNotFound;
        }

        return catalogEvent.Publish(time);
    }
}
