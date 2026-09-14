using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>
/// A concert, conference or workshop at a venue. Everything that shapes what is sold (seat map,
/// prices, on-sale window) can change only while the event is a draft: once it is published,
/// buyers may already be relying on it.
/// </summary>
internal sealed class Event : AggregateRoot<EventId>
{
    private readonly Dictionary<SectionCode, Money> _priceTiers = new();

    private Event(
        EventId id,
        OrganiserId organiserId,
        VenueId venueId,
        string title,
        DateTimeOffset startsAt,
        CurrencyCode currency,
        SeatMap seatMap)
        : base(id)
    {
        OrganiserId = organiserId;
        VenueId = venueId;
        Title = title;
        StartsAt = startsAt;
        Currency = currency;
        SeatMap = seatMap;
        Status = EventStatus.Draft;
    }

    public OrganiserId OrganiserId { get; }

    public VenueId VenueId { get; }

    public string Title { get; }

    public DateTimeOffset StartsAt { get; }

    public CurrencyCode Currency { get; }

    public SeatMap SeatMap { get; private set; }

    public OnSaleWindow? OnSaleWindow { get; private set; }

    public EventStatus Status { get; private set; }

    /// <summary>One price per section. Read-only: prices change only through <see cref="SetPriceTier"/>.</summary>
    public IReadOnlyDictionary<SectionCode, Money> PriceTiers => _priceTiers.AsReadOnly();

    /// <summary>Starts as a draft holding the venue's current seat map.</summary>
    public static Result<Event> Create(
        EventId id,
        OrganiserId organiserId,
        Venue venue,
        string? title,
        DateTimeOffset startsAt,
        CurrencyCode currency)
    {
        string trimmed = title?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return CatalogErrors.EventTitleRequired;
        }

        return new Event(id, organiserId, venue.Id, trimmed, startsAt, currency, venue.SeatMap);
    }

    /// <summary>Sets or replaces the price of one section.</summary>
    public Result SetPriceTier(SectionCode section, Money price)
    {
        if (Status != EventStatus.Draft)
        {
            return CatalogErrors.EventNotDraft;
        }

        if (!SeatMap.HasSection(section))
        {
            return CatalogErrors.UnknownSection(section);
        }

        if (price.Currency != Currency)
        {
            return CatalogErrors.CurrencyMismatch(price.Currency, Currency);
        }

        _priceTiers[section] = price;
        return Result.Success();
    }

    /// <summary>
    /// Refuses a seat map that would orphan a price tier rather than silently deleting the price:
    /// losing an organiser's pricing without telling them is worse than making them decide.
    /// </summary>
    public Result ReplaceSeatMap(SeatMap seatMap)
    {
        if (Status != EventStatus.Draft)
        {
            return CatalogErrors.EventNotDraft;
        }

        foreach (SectionCode pricedSection in _priceTiers.Keys)
        {
            if (!seatMap.HasSection(pricedSection))
            {
                return CatalogErrors.SeatMapDropsPricedSection(pricedSection);
            }
        }

        SeatMap = seatMap;
        return Result.Success();
    }

    public Result ScheduleOnSale(OnSaleWindow window)
    {
        if (Status != EventStatus.Draft)
        {
            return CatalogErrors.EventNotDraft;
        }

        if (window.ClosesAt > StartsAt)
        {
            return CatalogErrors.OnSaleClosesAfterStart;
        }

        OnSaleWindow = window;
        return Result.Success();
    }

    /// <summary>
    /// Publishing is the point of no return for what is sold, so it demands a complete event:
    /// a window that has not already closed, and a price for every section.
    /// </summary>
    public Result Publish(TimeProvider time)
    {
        if (Status != EventStatus.Draft)
        {
            return CatalogErrors.EventNotDraft;
        }

        if (OnSaleWindow is not { } window)
        {
            return CatalogErrors.NoOnSaleWindow;
        }

        if (window.ClosesAt <= time.GetUtcNow())
        {
            return CatalogErrors.OnSaleWindowAlreadyClosed;
        }

        foreach (Section section in SeatMap.Sections)
        {
            if (!_priceTiers.ContainsKey(section.Code))
            {
                return CatalogErrors.UnpricedSection(section.Code);
            }
        }

        Status = EventStatus.Published;
        Raise(new EventPublished(Id));
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == EventStatus.Cancelled)
        {
            return CatalogErrors.EventAlreadyCancelled;
        }

        Status = EventStatus.Cancelled;
        Raise(new EventCancelled(Id));
        return Result.Success();
    }

    public bool IsOnSale(TimeProvider time) =>
        Status == EventStatus.Published
        && OnSaleWindow is { } window
        && window.Contains(time.GetUtcNow());
}
