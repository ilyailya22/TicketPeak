using Shouldly;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.UnitTests.SharedKernel;

public sealed class AggregateRootTests
{
    [Fact]
    public void Raise_WhenBehaviourRuns_RecordsTheEvent()
    {
        Order order = new(new TestId(Guid.NewGuid()));

        order.Place();

        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderPlaced>();
    }

    [Fact]
    public void ClearDomainEvents_AfterRaise_RemovesAllEvents()
    {
        Order order = new(new TestId(Guid.NewGuid()));
        order.Place();

        order.ClearDomainEvents();

        order.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_WhenCastToMutableCollection_RefusesModification()
    {
        Order order = new(new TestId(Guid.NewGuid()));
        order.Place();

        ICollection<IDomainEvent> collection = order.DomainEvents.ShouldBeAssignableTo<ICollection<IDomainEvent>>()!;

        Should.Throw<NotSupportedException>(() => collection.Add(new OrderPlaced()));
    }

    private readonly record struct TestId(Guid Value) : IStronglyTypedId;

    private sealed record OrderPlaced : IDomainEvent;

    private sealed class Order(TestId id) : AggregateRoot<TestId>(id)
    {
        public void Place() => Raise(new OrderPlaced());
    }
}
