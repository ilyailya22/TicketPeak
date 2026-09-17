using FluentValidation;
using FluentValidation.Results;
using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Api.Behaviors;

/// <summary>
/// Refuses malformed input before any handler runs. It returns a validation failure instead of
/// throwing, because a client sending bad input is expected, not exceptional. Aggregates still
/// guard their own invariants: this catches shape, the domain catches rules.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        List<ValidationFailure> failures = [];

        foreach (IValidator<TRequest> validator in validators)
        {
            ValidationResult result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        string message = string.Join(" ", failures.Select(failure => failure.ErrorMessage));
        return ResultFailure<TResponse>.From(Error.Validation("Validation.Failed", message));
    }
}
