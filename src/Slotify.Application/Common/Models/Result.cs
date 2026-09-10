namespace Slotify.Application.Common.Models;

/// <summary>
/// Standardized API response wrapper for consistent error handling.
/// </summary>
public class Result<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];

    public static Result<T> Ok(T data, string? message = null) =>
        new() { Data = data, Success = true, Message = message };

    public static Result<T> Fail(string error) =>
        new() { Success = false, Errors = [error] };

    public static Result<T> Fail(List<string> errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Non-generic result for operations that don't return data.
/// </summary>
public class Result
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];

    public static Result Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static Result Fail(string error) =>
        new() { Success = false, Errors = [error] };

    public static Result Fail(List<string> errors) =>
        new() { Success = false, Errors = errors };
}
