using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>
/// When tickets may be bought. Half-open: from <see cref="OpensAt"/> up to, but not including,
/// <see cref="ClosesAt"/>, so back-to-back windows never overlap by an instant.
/// </summary>
internal readonly record struct OnSaleWindow
{
    private OnSaleWindow(DateTimeOffset opensAt, DateTimeOffset closesAt)
    {
        OpensAt = opensAt;
        ClosesAt = closesAt;
    }

    public DateTimeOffset OpensAt { get; }

    public DateTimeOffset ClosesAt { get; }

    public static Result<OnSaleWindow> Create(DateTimeOffset opensAt, DateTimeOffset closesAt)
    {
        if (opensAt >= closesAt)
        {
            return CatalogErrors.InvalidOnSaleWindow;
        }

        return new OnSaleWindow(opensAt, closesAt);
    }

    public bool Contains(DateTimeOffset instant) => instant >= OpensAt && instant < ClosesAt;
}
