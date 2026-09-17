namespace TicketPeak.Modules.Catalog;

/// <summary>A row of seats numbered 1 to <see cref="SeatCount"/>.</summary>
public sealed record SeatRowLayout(string Section, string Row, int SeatCount);
