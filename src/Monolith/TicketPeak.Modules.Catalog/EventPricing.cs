namespace TicketPeak.Modules.Catalog;

/// <summary>An event's price per section, in minor units of its one currency.</summary>
public sealed record EventPricing(Guid EventId, string Currency, IReadOnlyDictionary<string, long> PriceBySection);
