namespace SmartSchool.SharedKernel;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess == (error != Error.None))
        {
            throw new ArgumentException("A successful result cannot contain an error, and a failed result must contain one.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public sealed class Result<T>(T? value, bool isSuccess, Error error) : Result(isSuccess, error)
{
    private readonly T? _value = value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");
}
