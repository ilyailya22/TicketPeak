using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Domain;

/// <summary>Every expected failure in the Catalog domain. Codes are stable; messages are for humans.</summary>
internal static class CatalogErrors
{
    public static Error InvalidSectionCode { get; } =
        Error.Validation("Catalog.SectionCode.Invalid", "A section code must be 1 to 20 characters.");

    public static Error InvalidCurrency { get; } =
        Error.Validation("Catalog.Currency.Invalid", "A currency must be a three-letter ISO 4217 code.");

    public static Error NegativePrice { get; } =
        Error.Validation("Catalog.Price.Negative", "A price cannot be negative.");

    public static Error RowLabelRequired { get; } =
        Error.Validation("Catalog.Row.LabelRequired", "A row needs a label.");

    public static Error InvalidSeatCount { get; } =
        Error.Validation("Catalog.Row.InvalidSeatCount", "A row must have at least one seat.");

    public static Error SectionHasNoRows { get; } =
        Error.Validation("Catalog.Section.NoRows", "A reserved section must have at least one row.");

    public static Error InvalidCapacity { get; } =
        Error.Validation("Catalog.Section.InvalidCapacity", "A general admission section must hold at least one person.");

    public static Error SeatMapHasNoSections { get; } =
        Error.Validation("Catalog.SeatMap.NoSections", "A seat map must have at least one section.");

    public static Error InvalidOnSaleWindow { get; } =
        Error.Validation("Catalog.OnSaleWindow.Invalid", "Sales must open before they close.");

    public static Error VenueNameRequired { get; } =
        Error.Validation("Catalog.Venue.NameRequired", "A venue needs a name.");

    public static Error EventTitleRequired { get; } =
        Error.Validation("Catalog.Event.TitleRequired", "An event needs a title.");

    public static Error EventNotDraft { get; } =
        Error.Failure("Catalog.Event.NotDraft", "Only a draft event can be changed.");

    public static Error OnSaleClosesAfterStart { get; } =
        Error.Failure("Catalog.Event.OnSaleClosesAfterStart", "Sales must close no later than the event starts.");

    public static Error NoOnSaleWindow { get; } =
        Error.Failure("Catalog.Event.NoOnSaleWindow", "An event cannot be published without an on-sale window.");

    public static Error OnSaleWindowAlreadyClosed { get; } =
        Error.Failure("Catalog.Event.OnSaleWindowClosed", "An event cannot be published after its on-sale window has closed.");

    public static Error EventAlreadyCancelled { get; } =
        Error.Failure("Catalog.Event.AlreadyCancelled", "The event is already cancelled.");

    public static Error DuplicateRow(string label) =>
        Error.Validation("Catalog.Section.DuplicateRow", $"Row '{label}' appears more than once in the section.");

    public static Error DuplicateSection(SectionCode code) =>
        Error.Validation("Catalog.SeatMap.DuplicateSection", $"Section '{code}' appears more than once.");

    public static Error UnknownSection(SectionCode code) =>
        Error.Failure("Catalog.Event.UnknownSection", $"The event's seat map has no section '{code}'.");

    public static Error CurrencyMismatch(CurrencyCode price, CurrencyCode eventCurrency) =>
        Error.Failure("Catalog.Event.CurrencyMismatch", $"Prices must be in {eventCurrency}, not {price}.");

    public static Error SeatMapDropsPricedSection(SectionCode code) =>
        Error.Failure("Catalog.Event.SeatMapDropsPricedSection", $"The new seat map has no section '{code}', which has a price tier.");

    public static Error UnpricedSection(SectionCode code) =>
        Error.Failure("Catalog.Event.UnpricedSection", $"Section '{code}' has no price tier.");
}
