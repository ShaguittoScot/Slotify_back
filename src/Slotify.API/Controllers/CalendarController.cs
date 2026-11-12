using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Features.Calendar.Commands;
using Slotify.Application.Features.Calendar.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para la gestión y consulta de eventos del calendario y citas.
/// Expone endpoints para obtener slots agregados (citas y bloqueos) y registrar citas.
/// Relacionado con: US-003 (Calendario Táctil Multiformato), US-022 (Aislamiento por negocio).
/// 
/// Endpoints:
/// - GET  /api/calendar/slots        → Citas y bloqueos en un rango de fechas
/// - POST /api/calendar/appointments → Registrar nueva cita manual
/// 
/// Requiere autenticación JWT. El BusinessId se extrae exclusivamente del token mediante ICurrentUserService.
/// </summary>
[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Consulta los slots de la agenda (citas y bloqueos) para el rango de fechas indicado.
    /// Si el negocio no tiene citas, retorna una lista vacía para permitir renderizar el estado vacío.
    /// </summary>
    [HttpGet("slots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken)
    {
        var query = new GetCalendarSlotsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            EmployeeId = employeeId
        };

        var result = await _mediator.Send(query, cancellationToken);
        if (!result.Success)
            return StatusCode(StatusCodes.Status403Forbidden, result);

        return Ok(result);
    }

    /// <summary>
    /// Crea una nueva cita en el calendario del negocio autenticado.
    /// Despachada a través de MediatR hacia Slotify.Application cumpliendo CQRS.
    /// </summary>
    [HttpPost("appointments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(r => Ok(new { success = true, id = r.Data, data = r.Data }));
    }
}
