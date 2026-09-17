namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>A standing section's total capacity, as Catalog published it.</summary>
internal readonly record struct GeneralAdmissionLayout(string Section, int Capacity);
