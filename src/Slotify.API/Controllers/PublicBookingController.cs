using System.Net.Http.Headers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.API.Services;
using Slotify.Application.Features.PublicBooking;

namespace Slotify.API.Controllers;

/// <summary>
/// Endpoints públicos para el portal web de reservas (/book/{slug}) y widgets de agendamiento para clientes finales.
/// A diferencia de /api/businesses/{slug} (orientado al directorio general), este controlador provee los contratos
/// optimizados para la experiencia de auto-agendamiento (branding, slots de disponibilidad sin PII y citas públicas).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/businesses")]
public sealed class PublicBookingController(
    IMediator mediator,
    ISupabaseIdentityVerifier supabase) : ControllerBase
{
    /// <summary>
    /// Obtiene la información del negocio adaptada específicamente al flujo de reservas públicas web (/book/{slug}).
    /// Incluye identidad de marca (logo, color primario), servicios activos y configuración del formulario.
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBusiness(string slug, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetPublicBusinessBookingQuery(slug), cancellationToken)).ToActionResult();

    /// <summary>
    /// Consulta los intervalos de horarios disponibles de un negocio para una fecha específica (YYYY-MM-DD).
    /// Calcula la disponibilidad en tiempo real considerando horarios de atención, citas ocupadas y bloqueos de agenda,
    /// sin exponer información privada (PII) de empleados ni de otros clientes.
    /// </summary>
    [HttpGet("{slug}/availability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailability(
        string slug,
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
            return BadRequest(new { success = false, error = "Indica una fecha válida con formato YYYY-MM-DD." });

        return (await mediator.Send(new GetPublicAvailabilityQuery(slug, parsedDate), cancellationToken))
            .ToActionResult();
    }

    /// <summary>
    /// Registra una nueva cita desde el portal público de reservas (/book/{slug}).
    /// Soporta clientes anónimos/invitados (usando correo y teléfono provistos en el formulario)
    /// o clientes autenticados en Supabase (si envían un Bearer token en el encabezado Authorization).
    /// </summary>
    [HttpPost("{slug}/appointments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAppointment(
        string slug,
        [FromBody] CreatePublicAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        Guid? clientId = null;
        string? email = command.ClientEmail;

        if (AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) &&
            string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(header.Parameter))
        {
            try
            {
                var identity = await supabase.VerifyAsync(header.Parameter, cancellationToken);
                if (identity is not null)
                {
                    clientId = identity.Id;
                    if (string.IsNullOrWhiteSpace(email))
                        email = identity.Email;
                }
            }
            catch
            {
                // Las reservas públicas también permiten invitados sin sesión válida.
            }
        }

        var normalized = command with { Slug = slug, ClientId = clientId, ClientEmail = email };
        return (await mediator.Send(normalized, cancellationToken)).ToActionResult();
    }
}
