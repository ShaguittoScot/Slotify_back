using MediatR;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.Auth.Commands;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller de autenticación para administradores de negocio.
/// Relacionado con: US-000 (Registro y Autenticación de Administrador)
/// 
/// Endpoints:
/// - POST /api/auth/sync → Sincronizar nuevo admin + negocio tras registro en Supabase
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Registra un nuevo administrador y crea su negocio.
    /// </summary>
    /// <remarks>
    /// Crea un usuario con rol DUENO y un negocio asociado.
    /// Opcionalmente acepta un SectorTemplateId (seleccionado en US-006).
    /// Retorna los datos básicos del usuario creado en la BD relacional.
    /// </remarks>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Sync(
        [FromBody] RegisterAdminCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }


}
