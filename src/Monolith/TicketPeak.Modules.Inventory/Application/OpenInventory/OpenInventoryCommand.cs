using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Application.OpenInventory;

/// <summary>
/// Releases a published event's tickets for sale by building its inventory from Catalog's layout.
/// An explicit step for now; Phase 6 replaces it with a consumer of Catalog's EventPublished event.
/// </summary>
internal sealed record OpenInventoryCommand(Guid EventId) : IRequest<Result>, ICommand;
