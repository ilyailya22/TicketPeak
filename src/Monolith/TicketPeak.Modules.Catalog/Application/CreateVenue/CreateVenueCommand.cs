using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Modules.Catalog.Application.CreateVenue;

internal sealed record CreateVenueCommand(string Name, IReadOnlyList<SectionInput> Sections)
    : IRequest<Result<Guid>>, ICommand;

/// <summary>A section is reserved when it has rows and standing when it has a capacity: exactly one of the two.</summary>
internal sealed record SectionInput(string Code, IReadOnlyList<RowInput>? Rows, int? StandingCapacity);

internal sealed record RowInput(string Label, int Seats);
