
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.Onboarding.Commands;

namespace Slotify.API.Controllers;

/// <summary>
/// Gestiona la configuración inicial de un negocio.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OnboardingController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Completa la configuración inicial del negocio del administrador.
    /// </summary>
    [HttpPost("complete-business-onboarding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteBusinessOnboarding(
        [FromBody] CompleteBusinessOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}
