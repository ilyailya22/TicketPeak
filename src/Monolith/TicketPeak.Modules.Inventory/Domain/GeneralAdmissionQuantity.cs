namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>How many standing places a buyer wants in one section.</summary>
internal readonly record struct GeneralAdmissionQuantity(string Section, int Quantity);
