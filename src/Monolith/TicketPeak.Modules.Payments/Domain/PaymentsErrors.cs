using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Payments.Domain;

/// <summary>Every expected failure in the Payments domain. Codes are stable; messages are for humans.</summary>
internal static class PaymentsErrors
{
    public static Error NothingToCollect { get; } =
        Error.Validation("Payments.Intent.NothingToCollect", "A free order has nothing to collect and needs no payment.");

    public static Error ProviderReferenceRequired { get; } =
        Error.Validation("Payments.Intent.ProviderReferenceRequired", "An authorization must carry the provider's reference.");

    public static Error FailureReasonRequired { get; } =
        Error.Validation("Payments.Intent.FailureReasonRequired", "A failed payment must say why it failed.");

    public static Error AlreadyDecided(PaymentStatus status) =>
        Error.Failure("Payments.Intent.AlreadyDecided", $"The payment is already {status}.");
}
