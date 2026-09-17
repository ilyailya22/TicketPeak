namespace TicketPeak.Shared.Kernel;

/// <summary>An object defined by its identity rather than its attributes.</summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IStronglyTypedId
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; }

    public bool Equals(Entity<TId>? other) =>
        other is not null
        && (ReferenceEquals(this, other) || (GetType() == other.GetType() && Id.Equals(other.Id)));

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
