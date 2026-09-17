using MediatR;
using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.ScheduleOnSale;

internal sealed class ScheduleOnSaleHandler(IEventRepository events) : IRequestHandler<ScheduleOnSaleCommand, Result>
{
    public async Task<Result> Handle(ScheduleOnSaleCommand request, CancellationToken cancellationToken)
    {
        Event? catalogEvent = await events.FindAsync(new EventId(request.EventId), cancellationToken);

        if (catalogEvent is null)
        {
            return CatalogApplicationErrors.EventNotFound;
        }

        Result<OnSaleWindow> window = OnSaleWindow.Create(request.OpensAt, request.ClosesAt);

        if (window.IsFailure)
        {
            return window.Error;
        }

        return catalogEvent.ScheduleOnSale(window.Value);
    }
}
