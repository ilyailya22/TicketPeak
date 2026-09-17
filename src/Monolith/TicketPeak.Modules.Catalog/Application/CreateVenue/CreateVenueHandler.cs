using MediatR;
using TicketPeak.Modules.Catalog.Domain;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.CreateVenue;

internal sealed class CreateVenueHandler(IVenueRepository venues, TimeProvider time)
    : IRequestHandler<CreateVenueCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateVenueCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Create(request));

    private Result<Guid> Create(CreateVenueCommand request)
    {
        Result<SeatMap> seatMap = SeatMapFactory.FromInput(request.Sections);

        if (seatMap.IsFailure)
        {
            return seatMap.Error;
        }

        VenueId id = new(Guid.CreateVersion7(time.GetUtcNow()));
        Result<Venue> venue = Venue.Create(id, request.Name, seatMap.Value);

        if (venue.IsFailure)
        {
            return venue.Error;
        }

        venues.Add(venue.Value);
        return id.Value;
    }
}
