using Shouldly;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.UnitTests.SharedKernel;

public sealed class ResultTests
{
    private static readonly Error _seatTaken = Error.Conflict("Inventory.SeatTaken", "Seat 14B is already held.");

    [Fact]
    public void Success_WhenCreated_IsSuccessWithNoError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_WhenCreated_IsFailureCarryingTheError()
    {
        var result = Result.Failure(_seatTaken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_seatTaken);
    }

    [Fact]
    public void Failure_WithNullError_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void Value_OnSuccess_ReturnsTheValue()
    {
        var result = Result.Success(42);

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Value_OnFailure_ThrowsNamingTheErrorCode()
    {
        var result = Result.Failure<int>(_seatTaken);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => result.Value);
        exception.Message.ShouldContain("Inventory.SeatTaken");
    }

    [Fact]
    public void ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<string> result = "14B";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("14B");
    }

    [Fact]
    public void ImplicitConversion_FromError_ProducesFailure()
    {
        Result<string> result = _seatTaken;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_seatTaken);
    }
}
