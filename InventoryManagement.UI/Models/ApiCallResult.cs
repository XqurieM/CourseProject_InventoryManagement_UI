namespace InventoryManagement.UI.Models;

public class ApiCallResult
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiCallResult Success(int statusCode = StatusCodes.Status200OK) =>
        new() { IsSuccess = true, StatusCode = statusCode };

    public static ApiCallResult Failure(int statusCode, string? errorMessage) =>
        new() { IsSuccess = false, StatusCode = statusCode, ErrorMessage = errorMessage };
}

public sealed class ApiCallResult<T> : ApiCallResult
{
    public T? Value { get; init; }

    public static ApiCallResult<T> Success(T value, int statusCode = StatusCodes.Status200OK) =>
        new() { IsSuccess = true, StatusCode = statusCode, Value = value };

    public new static ApiCallResult<T> Failure(int statusCode, string? errorMessage) =>
        new() { IsSuccess = false, StatusCode = statusCode, ErrorMessage = errorMessage };
}
