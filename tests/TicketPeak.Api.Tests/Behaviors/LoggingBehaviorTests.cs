using Microsoft.Extensions.Logging;
using Shouldly;
using TicketPeak.Api.Behaviors;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.Api.Tests.Behaviors;

public sealed class LoggingBehaviorTests
{
    private readonly ListLogger<LoggingBehavior<HoldSeats, Result>> _logger = new();

    [Fact]
    public async Task Handle_WhenTheHandlerRefuses_LogsAWarningNamingTheErrorCode()
    {
        LoggingBehavior<HoldSeats, Result> behavior = new(_logger);
        HandlerSpy<Result> handler = new(Result.Failure(Error.Conflict("Inventory.Seat.Unavailable", "Seat STALLS A-1 is already held or sold.")));

        await behavior.Handle(new HoldSeats(), handler.Invoke, TestContext.Current.CancellationToken);

        (LogLevel _, string message) = _logger.Entries.Where(entry => entry.Level == LogLevel.Warning).ShouldHaveSingleItem();
        message.ShouldContain("Inventory.Seat.Unavailable");
    }

    [Fact]
    public async Task Handle_WhenTheHandlerSucceeds_LogsOnlyInformation()
    {
        LoggingBehavior<HoldSeats, Result> behavior = new(_logger);
        HandlerSpy<Result> handler = new(Result.Success());

        await behavior.Handle(new HoldSeats(), handler.Invoke, TestContext.Current.CancellationToken);

        _logger.Entries.ShouldAllBe(entry => entry.Level == LogLevel.Information);
        _logger.Entries.Count.ShouldBe(2);
    }

    internal sealed record HoldSeats;
}
