using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Payments.Domain;

/// <summary>
/// One attempt to collect an order's amount from the payment provider (a fake one until later
/// phases). It is decided exactly once, authorized or failed; trying again is a new intent, so
/// every provider call stays individually auditable.
/// </summary>
internal sealed class PaymentIntent : AggregateRoot<PaymentIntentId>
{
    private PaymentIntent(PaymentIntentId id, OrderId orderId, Money amount)
        : base(id)
    {
        OrderId = orderId;
        Amount = amount;
        Status = PaymentStatus.Pending;
    }

    public OrderId OrderId { get; }

    public Money Amount { get; }

    public PaymentStatus Status { get; private set; }

    public string? ProviderReference { get; private set; }

    public string? FailureReason { get; private set; }

    public static Result<PaymentIntent> Create(PaymentIntentId id, OrderId orderId, Money amount)
    {
        if (amount.AmountMinor == 0)
        {
            return PaymentsErrors.NothingToCollect;
        }

        return new PaymentIntent(id, orderId, amount);
    }

    public Result Authorize(string? providerReference)
    {
        if (Status != PaymentStatus.Pending)
        {
            return PaymentsErrors.AlreadyDecided(Status);
        }

        string trimmed = providerReference?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return PaymentsErrors.ProviderReferenceRequired;
        }

        Status = PaymentStatus.Authorized;
        ProviderReference = trimmed;
        Raise(new PaymentAuthorized(Id, OrderId, Amount));
        return Result.Success();
    }

    public Result Fail(string? reason)
    {
        if (Status != PaymentStatus.Pending)
        {
            return PaymentsErrors.AlreadyDecided(Status);
        }

        string trimmed = reason?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return PaymentsErrors.FailureReasonRequired;
        }

        Status = PaymentStatus.Failed;
        FailureReason = trimmed;
        Raise(new PaymentFailed(Id, OrderId, trimmed));
        return Result.Success();
    }
}
