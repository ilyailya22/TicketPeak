using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Api.Http;

/// <summary>
/// Turns the <see cref="Result"/> a module's endpoint returns into an HTTP response, so modules
/// speak in domain outcomes and only the host knows about status codes. Success with a value is
/// 200, success without one is 204, and every failure is an RFC 9457 problem whose status follows
/// its <see cref="ErrorType"/>. Phase 5 adds the trace id to the problem.
/// </summary>
internal sealed class ResultEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        object? response = await next(context);

        if (response is not Result result)
        {
            return response;
        }

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        PropertyInfo? valueProperty = result.GetType().GetProperty(nameof(Result<object>.Value));

        return valueProperty is null
            ? TypedResults.NoContent()
            : TypedResults.Ok(valueProperty.GetValue(result));
    }

    private static ProblemHttpResult ToProblem(Error error)
    {
        int status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            title: error.Code,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
