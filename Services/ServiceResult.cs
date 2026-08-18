namespace TaskHub.Services;

public enum ServiceErrorType
{
    Validation = 1,
    NotFound = 2,
    Forbidden = 3,
    Conflict = 4,
    Unauthorized = 5
}

public class ServiceResult
{
    public bool Succeeded { get; init; }

    public ServiceErrorType? ErrorType { get; init; }

    public string? ErrorMessage { get; init; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }

    public static ServiceResult Success() => new() { Succeeded = true };

    public static ServiceResult Failure(
        ServiceErrorType errorType,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
        => new()
        {
            Succeeded = false,
            ErrorType = errorType,
            ErrorMessage = message,
            ValidationErrors = validationErrors
        };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }

    public static ServiceResult<T> Success(T value)
        => new()
        {
            Succeeded = true,
            Value = value
        };

    public static new ServiceResult<T> Failure(
        ServiceErrorType errorType,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
        => new()
        {
            Succeeded = false,
            ErrorType = errorType,
            ErrorMessage = message,
            ValidationErrors = validationErrors
        };
}
