using System.Globalization;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Inventory.Domain;

/// <summary>Every expected failure in the Inventory domain. Codes are stable; messages are for humans.</summary>
internal static class InventoryErrors
{
    public static Error EmptyLayout { get; } =
        Error.Validation("Inventory.Layout.Empty", "An event's inventory needs at least one seat or standing place.");

    public static Error InvalidSeatCount { get; } =
        Error.Validation("Inventory.Layout.InvalidSeatCount", "A row must have at least one seat.");

    public static Error InvalidCapacity { get; } =
        Error.Validation("Inventory.Layout.InvalidCapacity", "A standing section must hold at least one person.");

    public static Error EmptyHold { get; } =
        Error.Validation("Inventory.Hold.Empty", "A hold must include at least one ticket.");

    public static Error TooManyTickets { get; } =
        Error.Validation(
            "Inventory.Hold.TooManyTickets",
            string.Create(CultureInfo.InvariantCulture, $"A hold can include at most {EventInventory.MaxTicketsPerHold} tickets."));

    public static Error InvalidQuantity { get; } =
        Error.Validation("Inventory.Hold.InvalidQuantity", "A standing quantity must be at least one.");

    public static Error DuplicateHold { get; } =
        Error.Conflict("Inventory.Hold.Duplicate", "A hold with this id already exists.");

    public static Error HoldNotFound { get; } =
        Error.NotFound("Inventory.Hold.NotFound", "No hold with this id exists for the event.");

    public static Error HoldExpired { get; } =
        Error.Failure("Inventory.Hold.Expired", "The hold has expired and its tickets may already be held by someone else.");

    public static Error HoldReleased { get; } =
        Error.Failure("Inventory.Hold.Released", "The hold was released and can no longer be confirmed.");

    public static Error HoldAlreadyConfirmed { get; } =
        Error.Failure("Inventory.Hold.AlreadyConfirmed", "The hold has already been confirmed; its tickets are sold.");

    public static Error DuplicateRow(string section, string row) =>
        Error.Validation("Inventory.Layout.DuplicateRow", $"Row '{row}' appears more than once in section '{section}'.");

    public static Error DuplicateSection(string section) =>
        Error.Validation("Inventory.Layout.DuplicateSection", $"Section '{section}' is defined more than once.");

    public static Error DuplicateSeatInRequest(SeatLocation seat) =>
        Error.Validation("Inventory.Hold.DuplicateSeat", $"Seat {seat} is requested more than once.");

    public static Error DuplicateSectionInRequest(string section) =>
        Error.Validation("Inventory.Hold.DuplicateSection", $"Section '{section}' is requested more than once.");

    public static Error UnknownSeat(SeatLocation seat) =>
        Error.Failure("Inventory.Seat.Unknown", $"Seat {seat} does not exist at this event.");

    public static Error UnknownSection(string section) =>
        Error.Failure("Inventory.Section.Unknown", $"Standing section '{section}' does not exist at this event.");

    public static Error SeatUnavailable(SeatLocation seat) =>
        Error.Conflict("Inventory.Seat.Unavailable", $"Seat {seat} is already held or sold.");

    public static Error InsufficientCapacity(string section, int available) =>
        Error.Conflict(
            "Inventory.Section.InsufficientCapacity",
            string.Create(CultureInfo.InvariantCulture, $"Only {available} standing places remain in section '{section}'."));
}
