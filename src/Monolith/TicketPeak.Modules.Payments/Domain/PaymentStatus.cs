namespace TicketPeak.Modules.Payments.Domain;

/// <summary>Pending → Authorized, or Pending → Failed. Decided exactly once.</summary>
internal enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Failed = 2,
}
