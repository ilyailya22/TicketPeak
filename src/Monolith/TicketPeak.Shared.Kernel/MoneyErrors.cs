namespace TicketPeak.Shared.Kernel;

public static class MoneyErrors
{
    public static Error InvalidCurrency { get; } =
        Error.Validation("Money.InvalidCurrency", "A currency must be a three-letter ISO 4217 code.");

    public static Error Negative { get; } =
        Error.Validation("Money.Negative", "An amount of money cannot be negative.");
}
