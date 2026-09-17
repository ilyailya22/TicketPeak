namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>
/// Stored state of a hold. "Expired" is deliberately absent: expiry is a function of the clock,
/// evaluated when asked, not a state something has to write.
/// </summary>
internal enum HoldStatus
{
    Active = 0,
    Released = 1,
    Confirmed = 2,
}
