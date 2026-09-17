using FluentValidation;
using Shouldly;
using TicketPeak.Api.Behaviors;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.Api.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenAValidatorFails_ReturnsAValidationFailureWithoutCallingTheHandler()
    {
        ValidationBehavior<RenameVenue, Result<Guid>> behavior = new([VenueRules()]);
        HandlerSpy<Result<Guid>> handler = new(Result.Success(Guid.NewGuid()));

        Result<Guid> result = await behavior.Handle(new RenameVenue("", 100), handler.Invoke, TestContext.Current.CancellationToken);

        Error error = result.Error.ShouldNotBeNull();
        error.Type.ShouldBe(ErrorType.Validation);
        error.Message.ShouldContain("A venue needs a name.");
        handler.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenSeveralRulesFail_ReportsEveryMessage()
    {
        ValidationBehavior<RenameVenue, Result<Guid>> behavior = new([VenueRules()]);
        HandlerSpy<Result<Guid>> handler = new(Result.Success(Guid.NewGuid()));

        Result<Guid> result = await behavior.Handle(new RenameVenue("", 0), handler.Invoke, TestContext.Current.CancellationToken);

        Error error = result.Error.ShouldNotBeNull();
        error.Message.ShouldContain("A venue needs a name.");
        error.Message.ShouldContain("A venue must hold at least one person.");
    }

    [Fact]
    public async Task Handle_WhenEveryValidatorPasses_CallsTheHandlerAndReturnsItsResult()
    {
        ValidationBehavior<RenameVenue, Result<Guid>> behavior = new([VenueRules()]);
        var id = Guid.NewGuid();
        HandlerSpy<Result<Guid>> handler = new(Result.Success(id));

        Result<Guid> result = await behavior.Handle(new RenameVenue("Roundhouse", 100), handler.Invoke, TestContext.Current.CancellationToken);

        result.Value.ShouldBe(id);
        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithNoValidators_CallsTheHandler()
    {
        ValidationBehavior<RenameVenue, Result<Guid>> behavior = new([]);
        HandlerSpy<Result<Guid>> handler = new(Result.Success(Guid.NewGuid()));

        await behavior.Handle(new RenameVenue("", 0), handler.Invoke, TestContext.Current.CancellationToken);

        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ForARequestReturningAPlainResult_BuildsAFailedResult()
    {
        ValidationBehavior<RenameVenue, Result> behavior = new([VenueRules()]);
        HandlerSpy<Result> handler = new(Result.Success());

        Result result = await behavior.Handle(new RenameVenue("", 100), handler.Invoke, TestContext.Current.CancellationToken);

        result.Error.ShouldNotBeNull().Type.ShouldBe(ErrorType.Validation);
        handler.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenValid_PassesTheCancellationTokenOnToTheHandler()
    {
        ValidationBehavior<RenameVenue, Result<Guid>> behavior = new([VenueRules()]);
        HandlerSpy<Result<Guid>> handler = new(Result.Success(Guid.NewGuid()));
        using CancellationTokenSource cancellation = new();

        await behavior.Handle(new RenameVenue("Roundhouse", 100), handler.Invoke, cancellation.Token);

        handler.ReceivedToken.ShouldBe(cancellation.Token);
    }

    private static InlineValidator<RenameVenue> VenueRules()
    {
        InlineValidator<RenameVenue> validator = new();
        validator.RuleFor(request => request.Name).NotEmpty().WithMessage("A venue needs a name.");
        validator.RuleFor(request => request.Capacity).GreaterThan(0).WithMessage("A venue must hold at least one person.");
        return validator;
    }

    internal sealed record RenameVenue(string Name, int Capacity);
}
