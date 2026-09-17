using FluentValidation;

namespace TicketPeak.Modules.Ordering.Application.PlaceOrder;

internal sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.HoldId).NotEmpty();
    }
}
