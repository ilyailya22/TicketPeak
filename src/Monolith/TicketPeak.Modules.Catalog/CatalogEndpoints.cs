using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketPeak.Modules.Catalog.Application.CreateEvent;
using TicketPeak.Modules.Catalog.Application.CreateVenue;
using TicketPeak.Modules.Catalog.Application.PublishEvent;
using TicketPeak.Modules.Catalog.Application.ScheduleOnSale;
using TicketPeak.Modules.Catalog.Application.SetPriceTier;
using TicketPeak.Modules.Catalog.Endpoints;

namespace TicketPeak.Modules.Catalog;

/// <summary>
/// Catalog's HTTP surface. Each endpoint sends one request and returns its Result unchanged; the
/// host's endpoint filter chooses the status code, so no module knows how errors map to HTTP.
/// </summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder venues = api.MapGroup("/venues").WithTags("Catalog");

        venues.MapPost(
                "/",
                ([FromBody] CreateVenueCommand command, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(command, cancellationToken))
            .WithName("CreateVenue");

        RouteGroupBuilder events = api.MapGroup("/events").WithTags("Catalog");

        events.MapPost(
                "/",
                ([FromBody] CreateEventCommand command, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(command, cancellationToken))
            .WithName("CreateEvent");

        events.MapPut(
                "/{eventId:guid}/prices/{section}",
                (Guid eventId, string section, [FromBody] PriceInput price, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(new SetPriceTierCommand(eventId, section, price.AmountMinor, price.Currency), cancellationToken))
            .WithName("SetPriceTier");

        events.MapPut(
                "/{eventId:guid}/on-sale",
                (Guid eventId, [FromBody] OnSaleInput window, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(new ScheduleOnSaleCommand(eventId, window.OpensAt, window.ClosesAt), cancellationToken))
            .WithName("ScheduleOnSale");

        events.MapPost(
                "/{eventId:guid}/publish",
                (Guid eventId, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(new PublishEventCommand(eventId), cancellationToken))
            .WithName("PublishEvent");

        return api;
    }
}
