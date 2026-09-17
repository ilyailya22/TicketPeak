using Shouldly;
using TicketPeak.Api.Behaviors;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.Api.Tests.Behaviors;

public sealed class UnitOfWorkBehaviorTests
{
    [Fact]
    public async Task Handle_WhenTheCommandSucceeds_SavesEveryModulesUnitOfWork()
    {
        RecordingUnitOfWork catalog = new();
        RecordingUnitOfWork inventory = new();
        UnitOfWorkBehavior<PublishEvent, Result> behavior = new([catalog, inventory]);
        HandlerSpy<Result> handler = new(Result.Success());

        await behavior.Handle(new PublishEvent(), handler.Invoke, TestContext.Current.CancellationToken);

        catalog.SaveCount.ShouldBe(1);
        inventory.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WhenTheCommandIsRefused_SavesNothing()
    {
        RecordingUnitOfWork catalog = new();
        UnitOfWorkBehavior<PublishEvent, Result> behavior = new([catalog]);
        HandlerSpy<Result> handler = new(Result.Failure(Error.Failure("Catalog.Event.NotDraft", "Only a draft event can be changed.")));

        Result result = await behavior.Handle(new PublishEvent(), handler.Invoke, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        catalog.SaveCount.ShouldBe(0);
    }

    internal sealed record PublishEvent : ICommand;
}
