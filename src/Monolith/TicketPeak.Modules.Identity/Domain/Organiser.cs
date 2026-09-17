using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Identity.Domain;

/// <summary>
/// A company or person who runs events. Deliberately thin: authentication, users and roles arrive
/// in Phase 3. This is the identity other modules refer to by id.
/// </summary>
internal sealed class Organiser : AggregateRoot<OrganiserId>
{
    private Organiser(OrganiserId id, string name, string contactEmail)
        : base(id)
    {
        Name = name;
        ContactEmail = contactEmail;
    }

    public string Name { get; }

    public string ContactEmail { get; }

    /// <remarks>
    /// The email check is shape only. Whether an address can receive mail is proven by sending
    /// mail, not by a regular expression, so the domain refuses only what is obviously not an address.
    /// </remarks>
    public static Result<Organiser> Create(OrganiserId id, string? name, string? contactEmail)
    {
        string trimmedName = name?.Trim() ?? string.Empty;

        if (trimmedName.Length == 0)
        {
            return IdentityErrors.NameRequired;
        }

        string email = contactEmail?.Trim() ?? string.Empty;
        int at = email.IndexOf('@');

        if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1)
        {
            return IdentityErrors.InvalidEmail;
        }

        return new Organiser(id, trimmedName, email);
    }
}
