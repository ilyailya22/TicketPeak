using FluentValidation;

namespace TicketPeak.Modules.Catalog.Application.CreateVenue;

internal sealed class CreateVenueValidator : AbstractValidator<CreateVenueCommand>
{
    public CreateVenueValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Sections).NotEmpty();
        RuleForEach(command => command.Sections)
            .Must(section => (section.Rows is { Count: > 0 }) != (section.StandingCapacity is not null))
            .WithMessage("Each section needs either rows of seats or a standing capacity, not both.");
    }
}
