using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.ScheduleBlocks.Commands;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para bloqueos manuales de agenda.
/// Relacionado con: US-008 (Bloqueo Manual de Horarios No Disponibles)
/// 
/// Endpoints:
/// - POST   /api/schedule-blocks     → Crear bloqueo
/// - DELETE /api/schedule-blocks/{id} → Eliminar bloqueo (desbloquear)
/// 
/// NOTA: Requiere autenticación JWT. El BusinessId se extrae del token.
/// TODO: Agregar atributo [Authorize] cuando se implemente el middleware JWT.
/// </summary>
[ApiController]
[Route("api/schedule-blocks")]
// [Authorize] // TODO: Descomentar cuando se configure el middleware de autenticación JWT
public class ScheduleBlocksController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Crea un nuevo bloqueo en la agenda.
    /// </summary>
    /// <remarks>
    /// Bloquea un rango de tiempo para que no se puedan agendar citas.
    /// Puede aplicar a un empleado específico o a todo el negocio.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateScheduleBlockCommand command,
        CancellationToken cancellationToken)
    {
        // TODO: Sobreescribir command.BusinessId con el valor del JWT
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(Create), new { id = result.Data }, result);
    }

    /// <summary>
    /// Elimina un bloqueo existente (desbloquea el horario).
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        // TODO: Extraer BusinessId del claim del JWT
        var businessId = Guid.Empty; // Placeholder

        var command = new DeleteScheduleBlockCommand
        {
            BlockId = id,
            BusinessId = businessId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
