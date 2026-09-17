namespace TicketPeak.Shared.Kernel;

/// <summary>
/// Something that happened inside an aggregate that other parts of the same module may react to.
/// Distinct from integration events (Phase 6), which cross module or service boundaries.
/// </summary>
public interface IDomainEvent;
