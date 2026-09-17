using Shouldly;
using TicketPeak.Modules.Payments.Domain;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.UnitTests.Payments;

public sealed class PaymentIntentTests
{
    [Fact]
    public void Create_ForAFreeOrder_IsRefused()
    {
        PaymentIntent.Create(NewId(), NewOrderId(), EurosOf(0)).Error.ShouldBe(PaymentsErrors.NothingToCollect);
    }

    [Fact]
    public void Create_WhenValid_StartsPendingWithNothingRaised()
    {
        PaymentIntent intent = PendingIntent();

        intent.Status.ShouldBe(PaymentStatus.Pending);
        intent.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Authorize_WhenPending_BecomesAuthorizedAndRaisesPaymentAuthorized()
    {
        PaymentIntent intent = PendingIntent();

        intent.Authorize(" psp_123 ").IsSuccess.ShouldBeTrue();

        intent.Status.ShouldBe(PaymentStatus.Authorized);
        intent.ProviderReference.ShouldBe("psp_123");
        PaymentAuthorized raised = intent.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PaymentAuthorized>();
        raised.OrderId.ShouldBe(intent.OrderId);
        raised.Amount.ShouldBe(intent.Amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Authorize_WithoutAProviderReference_IsRefused(string? providerReference)
    {
        PaymentIntent intent = PendingIntent();

        intent.Authorize(providerReference).Error.ShouldBe(PaymentsErrors.ProviderReferenceRequired);
        intent.Status.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void Authorize_Twice_IsRefused()
    {
        PaymentIntent intent = PendingIntent();
        intent.Authorize("psp_123");

        intent.Authorize("psp_456").Error.ShouldNotBeNull().Code.ShouldBe("Payments.Intent.AlreadyDecided");
        intent.ProviderReference.ShouldBe("psp_123");
    }

    [Fact]
    public void Authorize_AfterFailing_IsRefused()
    {
        PaymentIntent intent = PendingIntent();
        intent.Fail("card declined");

        intent.Authorize("psp_123").Error.ShouldNotBeNull().Code.ShouldBe("Payments.Intent.AlreadyDecided");
    }

    [Fact]
    public void Fail_WhenPending_BecomesFailedAndRaisesPaymentFailed()
    {
        PaymentIntent intent = PendingIntent();

        intent.Fail("card declined").IsSuccess.ShouldBeTrue();

        intent.Status.ShouldBe(PaymentStatus.Failed);
        intent.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PaymentFailed>().Reason.ShouldBe("card declined");
    }

    [Fact]
    public void Fail_WithoutAReason_IsRefused()
    {
        PendingIntent().Fail(" ").Error.ShouldBe(PaymentsErrors.FailureReasonRequired);
    }

    [Fact]
    public void Fail_AfterAuthorizing_IsRefused()
    {
        PaymentIntent intent = PendingIntent();
        intent.Authorize("psp_123");

        intent.Fail("card declined").Error.ShouldNotBeNull().Code.ShouldBe("Payments.Intent.AlreadyDecided");
        intent.Status.ShouldBe(PaymentStatus.Authorized);
    }

    private static PaymentIntentId NewId() => new(Guid.NewGuid());

    private static OrderId NewOrderId() => new(Guid.NewGuid());

    private static Money EurosOf(long amountMinor) => Money.Create(amountMinor, CurrencyCode.Create("EUR").Value).Value;

    private static PaymentIntent PendingIntent() => PaymentIntent.Create(NewId(), NewOrderId(), EurosOf(7500)).Value;
}
