namespace TicketPeak.Shared.Kernel;

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(error: null) => _value = value;

    private Result(Error error)
        : base(error)
    {
    }

    /// <summary>
    /// The success value. Reading it from a failed result is a programming error, not an expected
    /// outcome, so it throws rather than returning a default the caller might act on.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}).");

    public static implicit operator Result<T>(T value) => FromValue(value);

    public static implicit operator Result<T>(Error error) => FromError(error);

    internal static Result<T> FromValue(T value) => new(value);

    internal static new Result<T> FromError(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(error);
    }
}
