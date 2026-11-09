using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Slotify.API.Common;

/// <summary>
/// Manejador global de excepciones. Evita bloques try/catch en los controllers y
/// convierte las excepciones no controladas en respuestas HTTP consistentes.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errors) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList()),
            _ => (
                StatusCodes.Status500InternalServerError,
                new List<string> { "Ocurrió un error inesperado." })
        };

        logger.Log(
            statusCode >= StatusCodes.Status500InternalServerError ? LogLevel.Error : LogLevel.Warning,
            exception,
            "Request {Method} {Path} failed with {StatusCode}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            statusCode);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new { success = false, errors, error = errors.FirstOrDefault() },
            cancellationToken);

        return true;
    }
}

