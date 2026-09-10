using MediatR;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.Auth.Commands;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller de autenticación para administradores de negocio.
/// Relacionado con: US-000 (Registro y Autenticación de Administrador)
/// 
/// Endpoints:
/// - POST /api/auth/register → Registrar nuevo admin + negocio
/// - POST /api/auth/login    → Iniciar sesión
/// 
/// NOTA: Estos endpoints son públicos (no requieren JWT).
/// TODO: Agregar endpoint de refresh token.
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
    /// Retorna tokens JWT para acceso inmediato.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterAdminCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Inicia sesión como administrador de negocio.
    /// </summary>
    /// <remarks>
    /// Valida credenciales y retorna tokens JWT.
    /// Actualiza la fecha de último inicio de sesión.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }
}
