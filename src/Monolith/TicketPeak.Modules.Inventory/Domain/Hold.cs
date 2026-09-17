using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>
/// Tickets set aside for one buyer while they check out. Lives inside <see cref="EventInventory"/>,
/// which is the only thing allowed to change it.
/// </summary>
internal sealed class Hold : Entity<HoldId>
{
    public Hold(
        HoldId id,
        IReadOnlyList<SeatLocation> seats,
        IReadOnlyDictionary<string, int> generalAdmission,
        DateTimeOffset expiresAt)
        : base(id)
    {
        Seats = seats;
        GeneralAdmission = generalAdmission;
        ExpiresAt = expiresAt;
        Status = HoldStatus.Active;
    }

    public IReadOnlyList<SeatLocation> Seats { get; }

    /// <summary>Standing places held, by section code.</summary>
    public IReadOnlyDictionary<string, int> GeneralAdmission { get; }

    public DateTimeOffset ExpiresAt { get; }

    public HoldStatus Status { get; private set; }

    public int TicketCount => Seats.Count + GeneralAdmission.Values.Sum();

    /// <summary>
    /// Expiry is evaluated here, when asked. A lapsed hold therefore frees its tickets the instant
    /// it lapses, with no background job to run late, fail, or double-release.
    /// </summary>
    public bool IsActiveAt(DateTimeOffset instant) => Status == HoldStatus.Active && instant < ExpiresAt;

    public void MarkReleased() => Status = HoldStatus.Released;

    public void MarkConfirmed() => Status = HoldStatus.Confirmed;
}
