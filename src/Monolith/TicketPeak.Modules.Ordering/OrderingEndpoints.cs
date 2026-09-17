using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketPeak.Modules.Ordering.Application.GetOrder;
using TicketPeak.Modules.Ordering.Application.PlaceOrder;

namespace TicketPeak.Modules.Ordering;

/// <summary>Ordering's HTTP surface. Endpoints return Results; the host maps them to status codes.</summary>
public static class OrderingEndpoints
{
    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder api)
    {
        RouteGroupBuilder orders = api.MapGroup("/orders").WithTags("Ordering");

        orders.MapPost(
                "/",
                ([FromBody] PlaceOrderCommand command, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(command, cancellationToken))
            .WithName("PlaceOrder");

        orders.MapGet(
                "/{orderId:guid}",
                (Guid orderId, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                    sender.Send(new GetOrderQuery(orderId), cancellationToken))
            .WithName("GetOrder");

        return api;
    }
}
