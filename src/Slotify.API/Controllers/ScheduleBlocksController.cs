using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.ScheduleBlocks.Commands;
using Slotify.Application.Features.ScheduleBlocks.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para bloqueos manuales de agenda.
/// Relacionado con: US-008 (Bloqueo Manual de Horarios No Disponibles) y US-022 (Negocio del token).
/// 
/// Endpoints:
/// - POST   /api/schedule-blocks      → Crear bloqueo
/// - GET    /api/schedule-blocks      → Listar bloqueos en rango
/// - DELETE /api/schedule-blocks/{id} → Eliminar bloqueo (desbloquear)
/// 
/// Requiere autenticación JWT. El BusinessId se extrae exclusivamente del token mediante ICurrentUserService.
/// </summary>
[ApiController]
[Route("api/schedule-blocks")]
[Authorize]
public class ScheduleBlocksController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Crea un nuevo bloqueo en la agenda del negocio autenticado.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateScheduleBlockCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(Create), new { id = result.Data }, result);
    }

    /// <summary>
    /// Obtiene la lista de bloqueos del negocio autenticado en un rango de fechas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken)
    {
        var query = new GetScheduleBlocksQuery
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
    /// Elimina un bloqueo existente validando que pertenezca al negocio del token.
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteScheduleBlockCommand
        {
            BlockId = id
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            if (result.Errors.Contains(DeleteScheduleBlockHandler.ForbiddenError))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { error = "No tienes permisos para eliminar un bloqueo de otro negocio." });
            }

            return NotFound(result);
        }

        return Ok(result);
    }
}
