using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Features.Calendar.Commands;
using Slotify.Application.Features.Calendar.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para la vista de calendario del negocio.
/// Relacionado con: US-003 (Navegación Multiformato en Calendario Táctil) y US-022 (Negocio del token).
/// 
/// Endpoints:
/// - GET /api/calendar/slots?startDate=...&amp;endDate=... → Obtener slots del calendario
/// - POST /api/calendar/appointments → Crear cita en el negocio autenticado
/// 
/// Requiere autenticación JWT. El BusinessId se extrae exclusivamente del token mediante ICurrentUserService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
        return result.ToActionResult();
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

    [HttpPatch("appointments/{id:guid}")]
    public async Task<IActionResult> UpdateAppointment(
        Guid id, [FromBody] AppointmentUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateAppointmentCommand(id, request.Status, request.StartTime, request.EndTime), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees(CancellationToken cancellationToken) =>
        (await _mediator.Send(new GetCalendarEmployeesQuery(), cancellationToken)).ToActionResult();

    public record AppointmentUpdateRequest(string? Status, DateTime? StartTime, DateTime? EndTime);
}
