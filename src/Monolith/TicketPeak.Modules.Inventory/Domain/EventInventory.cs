using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>
/// Everything sellable for one event, and the only place that decides whether a ticket can be held
/// or sold. One aggregate per event is the consistency boundary for the invariant that matters
/// most: a reserved seat is never held or sold twice, and a standing section never oversells.
/// </summary>
internal sealed class EventInventory : AggregateRoot<EventId>
{
    public const int MaxTicketsPerHold = 8;

    private readonly HashSet<SeatLocation> _seats;
    private readonly Dictionary<string, int> _capacity;
    private readonly HashSet<SeatLocation> _soldSeats = [];
    private readonly Dictionary<string, int> _soldGeneralAdmission = new(StringComparer.Ordinal);
    private readonly Dictionary<HoldId, Hold> _holds = [];

    private EventInventory(EventId id, HashSet<SeatLocation> seats, Dictionary<string, int> capacity)
        : base(id)
    {
        _seats = seats;
        _capacity = capacity;
    }

    public static TimeSpan HoldDuration { get; } = TimeSpan.FromMinutes(10);

    public static Result<EventInventory> Create(
        EventId id,
        IEnumerable<ReservedRowLayout> reservedRows,
        IEnumerable<GeneralAdmissionLayout> generalAdmission)
    {
        HashSet<SeatLocation> seats = [];
        HashSet<(string Section, string Row)> rows = [];
        HashSet<string> reservedSections = new(StringComparer.Ordinal);

        foreach (ReservedRowLayout layout in reservedRows)
        {
            if (layout.SeatCount < 1)
            {
                return InventoryErrors.InvalidSeatCount;
            }

            string section = SeatLocation.Normalize(layout.Section);
            string row = SeatLocation.Normalize(layout.Row);

            if (!rows.Add((section, row)))
            {
                return InventoryErrors.DuplicateRow(section, row);
            }

            reservedSections.Add(section);

            for (int number = 1; number <= layout.SeatCount; number++)
            {
                seats.Add(SeatLocation.Of(section, row, number));
            }
        }

        Dictionary<string, int> capacity = new(StringComparer.Ordinal);

        foreach (GeneralAdmissionLayout layout in generalAdmission)
        {
            if (layout.Capacity < 1)
            {
                return InventoryErrors.InvalidCapacity;
            }

            string section = SeatLocation.Normalize(layout.Section);

            if (reservedSections.Contains(section) || !capacity.TryAdd(section, layout.Capacity))
            {
                return InventoryErrors.DuplicateSection(section);
            }
        }

        if (seats.Count == 0 && capacity.Count == 0)
        {
            return InventoryErrors.EmptyLayout;
        }

        return new EventInventory(id, seats, capacity);
    }

    /// <summary>
    /// Sets tickets aside for <see cref="HoldDuration"/>. All-or-nothing: if any requested ticket is
    /// unavailable, nothing is held, so a buyer never ends up with half of what they asked for.
    /// </summary>
    public Result<Hold> PlaceHold(
        HoldId holdId,
        IReadOnlyCollection<SeatLocation> seats,
        IReadOnlyCollection<GeneralAdmissionQuantity> generalAdmission,
        TimeProvider time)
    {
        DateTimeOffset now = time.GetUtcNow();

        if (_holds.ContainsKey(holdId))
        {
            return InventoryErrors.DuplicateHold;
        }

        HashSet<SeatLocation> requestedSeats = [];

        foreach (SeatLocation seat in seats)
        {
            if (!requestedSeats.Add(seat))
            {
                return InventoryErrors.DuplicateSeatInRequest(seat);
            }
        }

        Dictionary<string, int> requestedStanding = new(StringComparer.Ordinal);

        foreach (GeneralAdmissionQuantity request in generalAdmission)
        {
            if (request.Quantity < 1)
            {
                return InventoryErrors.InvalidQuantity;
            }

            string section = SeatLocation.Normalize(request.Section);

            if (!requestedStanding.TryAdd(section, request.Quantity))
            {
                return InventoryErrors.DuplicateSectionInRequest(section);
            }
        }

        int ticketCount = requestedSeats.Count + requestedStanding.Values.Sum();

        if (ticketCount == 0)
        {
            return InventoryErrors.EmptyHold;
        }

        if (ticketCount > MaxTicketsPerHold)
        {
            return InventoryErrors.TooManyTickets;
        }

        foreach (SeatLocation seat in requestedSeats)
        {
            if (!_seats.Contains(seat))
            {
                return InventoryErrors.UnknownSeat(seat);
            }

            if (StatusAt(seat, now) != SeatStatus.Available)
            {
                return InventoryErrors.SeatUnavailable(seat);
            }
        }

        foreach ((string section, int quantity) in requestedStanding)
        {
            if (!_capacity.ContainsKey(section))
            {
                return InventoryErrors.UnknownSection(section);
            }

            int available = AvailableAt(section, now);

            if (quantity > available)
            {
                return InventoryErrors.InsufficientCapacity(section, available);
            }
        }

        Hold hold = new(holdId, [.. requestedSeats], requestedStanding.AsReadOnly(), now + HoldDuration);
        _holds.Add(holdId, hold);
        return hold;
    }

    /// <summary>
    /// Idempotent for anything not yet sold: releasing twice, or releasing a hold that already
    /// expired, leaves the tickets free either way, so a retried "cancel basket" never fails.
    /// </summary>
    public Result Release(HoldId holdId)
    {
        if (!_holds.TryGetValue(holdId, out Hold? hold))
        {
            return InventoryErrors.HoldNotFound;
        }

        if (hold.Status == HoldStatus.Confirmed)
        {
            return InventoryErrors.HoldAlreadyConfirmed;
        }

        hold.MarkReleased();
        return Result.Success();
    }

    /// <summary>Turns a live hold into a sale. Refused once the hold has expired, because its tickets may now belong to someone else.</summary>
    public Result Confirm(HoldId holdId, TimeProvider time)
    {
        if (!_holds.TryGetValue(holdId, out Hold? hold))
        {
            return InventoryErrors.HoldNotFound;
        }

        if (hold.Status == HoldStatus.Confirmed)
        {
            return InventoryErrors.HoldAlreadyConfirmed;
        }

        if (hold.Status == HoldStatus.Released)
        {
            return InventoryErrors.HoldReleased;
        }

        if (!hold.IsActiveAt(time.GetUtcNow()))
        {
            return InventoryErrors.HoldExpired;
        }

        foreach (SeatLocation seat in hold.Seats)
        {
            _soldSeats.Add(seat);
        }

        foreach ((string section, int quantity) in hold.GeneralAdmission)
        {
            _soldGeneralAdmission[section] = SoldIn(section) + quantity;
        }

        hold.MarkConfirmed();
        Raise(new HoldConfirmed(Id, holdId));
        return Result.Success();
    }

    public Result<Hold> FindHold(HoldId holdId)
    {
        if (!_holds.TryGetValue(holdId, out Hold? hold))
        {
            return InventoryErrors.HoldNotFound;
        }

        return hold;
    }

    public Result<SeatStatus> SeatStatusOf(SeatLocation seat, TimeProvider time)
    {
        if (!_seats.Contains(seat))
        {
            return InventoryErrors.UnknownSeat(seat);
        }

        return StatusAt(seat, time.GetUtcNow());
    }

    public Result<int> AvailableCapacityOf(string section, TimeProvider time)
    {
        string normalized = SeatLocation.Normalize(section);

        if (!_capacity.ContainsKey(normalized))
        {
            return InventoryErrors.UnknownSection(normalized);
        }

        return AvailableAt(normalized, time.GetUtcNow());
    }

    private SeatStatus StatusAt(SeatLocation seat, DateTimeOffset now)
    {
        if (_soldSeats.Contains(seat))
        {
            return SeatStatus.Sold;
        }

        return _holds.Values.Any(hold => hold.IsActiveAt(now) && hold.Seats.Contains(seat))
            ? SeatStatus.Held
            : SeatStatus.Available;
    }

    private int AvailableAt(string section, DateTimeOffset now)
    {
        int held = _holds.Values
            .Where(hold => hold.IsActiveAt(now))
            .Sum(hold => hold.GeneralAdmission.GetValueOrDefault(section));

        return _capacity[section] - SoldIn(section) - held;
    }

    private int SoldIn(string section) => _soldGeneralAdmission.GetValueOrDefault(section);
}
