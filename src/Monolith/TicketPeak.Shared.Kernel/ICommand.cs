namespace TicketPeak.Shared.Kernel;

/// <summary>
/// Marks a request that changes state. The unit-of-work pipeline behaviour is constrained to it, so
/// a query never saves anything: the container does not apply that behaviour to queries at all.
/// </summary>
public interface ICommand;
