using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketPeak.Modules.Inventory.Application.OpenInventory;
using TicketPeak.Modules.Inventory.Application.PlaceHold;
using TicketPeak.Modules.Inventory.Endpoints;

namespace TicketPeak.Modules.Inventory;

/// <summary>Inventory's HTTP surface. Endpoints return Results; the host maps them to status codes.</summary>
public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder events = api.MapGroup("/events").WithTags("Inventory");

        events.MapPost(
                "/{eventId:guid}/inventory",
                (Guid eventId, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(new OpenInventoryCommand(eventId), cancellationToken))
            .WithName("OpenInventory");

        events.MapPost(
                "/{eventId:guid}/holds",
                (Guid eventId, [FromBody] HoldInput hold, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(new PlaceHoldCommand(eventId, hold.Seats ?? [], hold.Standing ?? []), cancellationToken))
            .WithName("PlaceHold");

        return api;
    }
}
