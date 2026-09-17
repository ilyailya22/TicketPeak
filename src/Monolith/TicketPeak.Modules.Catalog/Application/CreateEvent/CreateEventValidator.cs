using FluentValidation;

namespace TicketPeak.Modules.Catalog.Application.CreateEvent;

internal sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(command => command.OrganiserId).NotEmpty();
        RuleFor(command => command.VenueId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty();
        RuleFor(command => command.Currency).NotEmpty();
    }
}
