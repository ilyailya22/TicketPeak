using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.SetPriceTier;

internal sealed record SetPriceTierCommand(Guid EventId, string Section, long AmountMinor, string Currency)
    : IRequest<Result>, ICommand;
