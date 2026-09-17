using System.Reflection;
using TicketPeak.Shared.Kernel;

namespace TicketPeak.Api.Behaviors;

/// <summary>
/// Builds a failed <typeparamref name="TResponse"/> whether it is <see cref="Result"/> or
/// <see cref="Result{T}"/>, so a behaviour can short-circuit without knowing which. The factory is
/// built once per response type and cached in this generic class's static field, so the reflection
/// runs once per type, not once per request.
/// </summary>
internal static class ResultFailure<TResponse>
    where TResponse : Result
{
    private static readonly Func<Error, TResponse> _create = BuildFactory();

    public static TResponse From(Error error) => _create(error);

    private static Func<Error, TResponse> BuildFactory()
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return error => (TResponse)Result.Failure(error);
        }

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            MethodInfo failure = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(Result.Failure) && method.IsGenericMethodDefinition)
                .MakeGenericMethod(typeof(TResponse).GetGenericArguments()[0]);

            return failure.CreateDelegate<Func<Error, TResponse>>();
        }

        throw new InvalidOperationException($"{typeof(TResponse).Name} is not a Result, so no failure can be built for it.");
    }
}
