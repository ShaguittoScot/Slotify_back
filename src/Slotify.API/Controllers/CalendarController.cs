using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.Calendar.Commands;
using Slotify.Application.Features.Calendar.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para la vista de calendario del negocio.
/// Relacionado con: US-003 (Navegación Multiformato en Calendario Táctil)
/// 
/// Endpoints:
/// - GET /api/calendar/slots?startDate=...&endDate=... → Obtener slots del calendario
/// 
/// NOTA: Requiere autenticación JWT. El BusinessId se extrae del token.
/// TODO: Agregar atributo [Authorize] cuando se implemente el middleware JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
// [Authorize] // TODO: Descomentar cuando se configure el middleware de autenticación JWT
public class CalendarController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Obtiene los slots del calendario para un rango de fechas.
    /// </summary>
    /// <remarks>
    /// Combina citas, bloqueos y disponibilidad en una vista unificada.
    /// El businessId se extrae del JWT del usuario autenticado.
    /// Soporta filtro opcional por empleado.
    /// </remarks>
    [HttpGet("slots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? businessId,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken)
    {
        var effectiveBusinessId = businessId ?? Guid.Empty;

        var query = new GetCalendarSlotsQuery
        {
            BusinessId = effectiveBusinessId,
            StartDate = startDate,
            EndDate = endDate,
            EmployeeId = employeeId
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Crea una nueva cita en el calendario.
    /// Despachada a través de MediatR hacia Slotify.Application cumpliendo CQRS.
    /// </summary>
    [HttpPost("appointments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.Success)
            return BadRequest(result);

        return Ok(new { success = true, id = result.Data, businessId = command.BusinessId, data = result.Data });
    }
}
