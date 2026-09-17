namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>A row of seats numbered 1 to <see cref="SeatCount"/>, as Catalog published it.</summary>
internal readonly record struct ReservedRowLayout(string Section, string Row, int SeatCount);
