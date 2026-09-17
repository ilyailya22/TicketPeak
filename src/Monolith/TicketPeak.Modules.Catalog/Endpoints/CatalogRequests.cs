namespace TicketPeak.Modules.Catalog.Endpoints;

/// <summary>Request bodies whose other half comes from the route, e.g. the event id and section.</summary>
internal sealed record PriceInput(long AmountMinor, string Currency);

internal sealed record OnSaleInput(DateTimeOffset OpensAt, DateTimeOffset ClosesAt);
