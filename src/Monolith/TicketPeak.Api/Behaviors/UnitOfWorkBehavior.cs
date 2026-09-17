using MediatR;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Api.Behaviors;

/// <summary>
/// Commits every module's unit of work after a command succeeds, and none after it fails, so a
/// refused business rule never leaves half its changes behind. Constrained to
/// <see cref="ICommand"/>: the container never applies it to a query, so there is no runtime check
/// to forget.
/// </summary>
internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IEnumerable<IUnitOfWork> unitsOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        TResponse response = await next(cancellationToken);

        if (response.IsFailure)
        {
            return response;
        }

        foreach (IUnitOfWork unitOfWork in unitsOfWork)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
