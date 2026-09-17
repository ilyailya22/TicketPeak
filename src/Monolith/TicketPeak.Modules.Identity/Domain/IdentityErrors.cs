using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Identity.Domain;

/// <summary>Every expected failure in the Identity domain. Codes are stable; messages are for humans.</summary>
internal static class IdentityErrors
{
    public static Error NameRequired { get; } =
        Error.Validation("Identity.Organiser.NameRequired", "An organiser needs a name.");

    public static Error InvalidEmail { get; } =
        Error.Validation("Identity.Organiser.InvalidEmail", "A contact email needs text either side of a single '@'.");
}
