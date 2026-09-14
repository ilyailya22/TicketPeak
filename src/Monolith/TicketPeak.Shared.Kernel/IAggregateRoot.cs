namespace TicketPeak.Shared.Kernel;

/// <summary>
/// Non-generic view of an aggregate, so the unit of work can collect domain events from every
/// tracked aggregate without knowing each one's identifier type.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
