using Shouldly;
using TicketPeak.Shared.Kernel;
using Xunit;

namespace TicketPeak.UnitTests.SharedKernel;

public sealed class EntityTests
{
    [Fact]
    public void Equals_SameTypeAndId_AreEqual()
    {
        TestId id = new(Guid.NewGuid());
        Seat first = new(id);
        Seat second = new(id);

        first.Equals(second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equals_SameTypeDifferentId_AreNotEqual()
    {
        Seat first = new(new TestId(Guid.NewGuid()));
        Seat second = new(new TestId(Guid.NewGuid()));

        first.Equals(second).ShouldBeFalse();
        (first != second).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentTypesSameId_AreNotEqual()
    {
        TestId id = new(Guid.NewGuid());
        Seat seat = new(id);
        Section section = new(id);

        seat.Equals(section).ShouldBeFalse();
    }

    [Fact]
    public void Equals_Null_IsFalse()
    {
        Seat seat = new(new TestId(Guid.NewGuid()));

        seat.Equals(null).ShouldBeFalse();
        (seat == null).ShouldBeFalse();
    }

    private readonly record struct TestId(Guid Value) : IStronglyTypedId;

    private sealed class Seat(TestId id) : Entity<TestId>(id);

    private sealed class Section(TestId id) : Entity<TestId>(id);
}
