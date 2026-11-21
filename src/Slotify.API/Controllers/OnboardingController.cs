using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Application.Features.Onboarding.Commands;

namespace Slotify.API.Controllers;

/// <summary>
/// Gestiona la configuración inicial de un negocio (Onboarding B2B).
/// Requiere autenticación con rol de administrador del negocio (Owner).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Completa la configuración inicial del negocio del administrador autenticado.
    /// Resuelve la identidad y la pertenencia del negocio exclusivamente desde los claims del token JWT.
    /// </summary>
    [HttpPost("complete-business-onboarding")]
    [ProducesResponseType(typeof(Result<AuthUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteBusinessOnboarding(
        [FromBody] CompleteBusinessOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}
