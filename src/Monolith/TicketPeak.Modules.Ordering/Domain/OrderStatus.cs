namespace TicketPeak.Modules.Ordering.Domain;

/// <summary>Placed → Paid → Completed; or Placed → Cancelled; or Placed → Expired.</summary>
internal enum OrderStatus
{
    Placed = 0,
    Paid = 1,
    Completed = 2,
    Cancelled = 3,
    Expired = 4,
}
