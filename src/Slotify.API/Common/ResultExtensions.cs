using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Common.Models;

namespace Slotify.API.Common;

/// <summary>
/// Traduce un <see cref="Result"/> / <see cref="Result{T}"/> de la capa de aplicación
/// a un <see cref="IActionResult"/> HTTP. Centraliza el mapeo para que los controllers
/// no contengan lógica condicional (solo orquestan: reciben, despachan y responden).
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        Func<Result<T>, IActionResult>? onSuccess = null) =>
        result.Success
            ? (onSuccess ?? (r => new OkObjectResult(r)))(result)
            : ToErrorResult(result.ErrorType, result.Errors, result.Message);

    public static IActionResult ToActionResult(
        this Result result,
        Func<Result, IActionResult>? onSuccess = null) =>
        result.Success
            ? (onSuccess ?? (r => new OkObjectResult(r)))(result)
            : ToErrorResult(result.ErrorType, result.Errors, result.Message);

    private static IActionResult ToErrorResult(ErrorType errorType, List<string> errors, string? message)
    {
        var body = new
        {
            success = false,
            message,
            errors,
            error = errors.FirstOrDefault()
        };

        return errorType switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(body),
            ErrorType.Conflict => new ConflictObjectResult(body),
            ErrorType.Forbidden => new ObjectResult(body) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new BadRequestObjectResult(body)
        };
    }
}

