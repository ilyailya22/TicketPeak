using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory;

/// <summary>
/// The only way another module may reach Inventory. Called synchronously from inside the checkout's
/// consistency boundary. See ADR 0004.
/// </summary>
public interface IInventoryApi
{
    /// <summary>Refuses a hold that has expired, been released or already been confirmed.</summary>
    Task<Result<ActiveHold>> GetActiveHoldAsync(Guid eventId, Guid holdId, CancellationToken cancellationToken);
}
