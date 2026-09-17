using System.Diagnostics.CodeAnalysis;

namespace TicketPeak.Shared.Kernel;

/// <summary>
/// The outcome of an operation that can fail for an expected reason. Exceptions are reserved for
/// the unexpected; a refused business rule is not exceptional.
/// </summary>
public class Result
{
    private static readonly Result _successInstance = new(error: null);

    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public static Result Success() => _successInstance;

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static Result<T> Success<T>(T value) => Result<T>.FromValue(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.FromError(error);

    public static implicit operator Result(Error error) => Failure(error);

    public static Result FromError(Error error) => Failure(error);
}
