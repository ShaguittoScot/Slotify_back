
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Slotify.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation error in request.");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Los datos enviados no son válidos.",
                errors = ex.Errors.Select(error => error.ErrorMessage).Distinct().ToList()
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception.");

            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Ocurrió un error interno en el servidor."
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}
