using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Ordering.Application.GetOrder;

/// <summary>A query: not an ICommand, so the unit-of-work behaviour is never applied to it.</summary>
internal sealed record GetOrderQuery(Guid OrderId) : IRequest<Result<OrderResponse>>;

internal sealed record OrderResponse(
    Guid OrderId,
    string Status,
    long TotalMinor,
    string Currency,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<string> Tickets);
