using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Features.Notifications.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para el motor de notificaciones push de Slotify.
/// US-Notifications: Provee citas próximas + snapshot hash para que el frontend
/// pueda programar recordatorios locales y detectar cambios en la agenda.
///
/// Endpoints:
/// - GET /api/notifications/upcoming?horizonDays=30
///   → Devuelve citas próximas + hash de snapshot
///
/// Accesible para: DUENO, EMPLEADO, CLIENTE (el handler filtra por rol).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Obtiene las citas próximas del usuario autenticado (negocio o cliente)
    /// y un hash del snapshot actual para detección de cambios.
    /// </summary>
    /// <param name="horizonDays">Días hacia adelante a consultar (1–90, default 30).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUpcoming(
        [FromQuery] int horizonDays = 30,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUpcomingNotificationsQuery { HorizonDays = horizonDays };
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult();
    }
}
