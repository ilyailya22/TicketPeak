using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Identity.Domain;

internal readonly record struct OrganiserId(Guid Value) : IStronglyTypedId;
