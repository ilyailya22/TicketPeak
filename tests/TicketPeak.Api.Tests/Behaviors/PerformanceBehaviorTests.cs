using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using TicketPeak.Api.Behaviors;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.Api.Tests.Behaviors;

public sealed class PerformanceBehaviorTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly ListLogger<PerformanceBehavior<SearchEvents, Result>> _logger = new();

    [Fact]
    public async Task Handle_WhenSlowerThanTheThreshold_LogsAWarning()
    {
        PerformanceBehavior<SearchEvents, Result> behavior = new(_logger, _time);

        await behavior.Handle(new SearchEvents(), _ => HandlerTaking(TimeSpan.FromMilliseconds(501)), TestContext.Current.CancellationToken);

        (LogLevel level, string message) = _logger.Entries.ShouldHaveSingleItem();
        level.ShouldBe(LogLevel.Warning);
        message.ShouldContain(nameof(SearchEvents));
    }

    [Fact]
    public async Task Handle_WhenExactlyAtTheThreshold_LogsNothing()
    {
        PerformanceBehavior<SearchEvents, Result> behavior = new(_logger, _time);

        await behavior.Handle(new SearchEvents(), _ => HandlerTaking(TimeSpan.FromMilliseconds(500)), TestContext.Current.CancellationToken);

        _logger.Entries.ShouldBeEmpty();
    }

    /// <summary>Simulates a handler's duration by moving the fake clock, so the test never sleeps.</summary>
    private Task<Result> HandlerTaking(TimeSpan duration)
    {
        _time.Advance(duration);
        return Task.FromResult(Result.Success());
    }

    internal sealed record SearchEvents;
}
