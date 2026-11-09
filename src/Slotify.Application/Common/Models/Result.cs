using System.Text.Json.Serialization;

namespace Slotify.Application.Common.Models;

/// <summary>
/// CategorÃ­a semÃ¡ntica del error. Permite que la capa de presentaciÃ³n traduzca
/// el resultado a un cÃ³digo HTTP sin lÃ³gica condicional en los controllers.
/// </summary>
public enum ErrorType
{
    None = 0,
    Failure,
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

/// <summary>
/// Standardized API response wrapper for consistent error handling.
/// </summary>
public class Result<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];
    [JsonIgnore]
    public ErrorType ErrorType { get; set; } = ErrorType.None;

    public static Result<T> Ok(T data, string? message = null) =>
        new() { Data = data, Success = true, Message = message };

    public static Result<T> Fail(string error, ErrorType errorType = ErrorType.Failure) =>
        new() { Success = false, Errors = [error], ErrorType = errorType };

    public static Result<T> Fail(List<string> errors, ErrorType errorType = ErrorType.Failure) =>
        new() { Success = false, Errors = errors, ErrorType = errorType };

    public static Result<T> NotFound(string error) => Fail(error, ErrorType.NotFound);
}

/// <summary>
/// Non-generic result for operations that don't return data.
/// </summary>
public class Result
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];
    [JsonIgnore]
    public ErrorType ErrorType { get; set; } = ErrorType.None;

    public static Result Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static Result Fail(string error, ErrorType errorType = ErrorType.Failure) =>
        new() { Success = false, Errors = [error], ErrorType = errorType };

    public static Result Fail(List<string> errors, ErrorType errorType = ErrorType.Failure) =>
        new() { Success = false, Errors = errors, ErrorType = errorType };

    public static Result NotFound(string error) => Fail(error, ErrorType.NotFound);
}
