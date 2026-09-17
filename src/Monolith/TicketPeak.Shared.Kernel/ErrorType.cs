namespace TicketPeak.Shared.Kernel;

/// <summary>
/// Classifies an expected failure so the API edge can choose a status code without the domain
/// knowing HTTP exists.
/// </summary>
public enum ErrorType
{
    /// <summary>A business rule refused the operation. Maps to 422.</summary>
    Failure = 0,

    /// <summary>The input was malformed before any rule ran. Maps to 400.</summary>
    Validation = 1,

    /// <summary>The target does not exist. Maps to 404.</summary>
    NotFound = 2,

    /// <summary>The operation lost a race with current state, e.g. a seat already held. Maps to 409.</summary>
    Conflict = 3,
}
