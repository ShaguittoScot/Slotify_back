using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Features.Auth.Commands;

namespace Slotify.API.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(IMediator mediator) : ControllerBase
{
    [HttpPatch]
    public async Task<IActionResult> Update(UpdateOwnProfileCommand command, CancellationToken token) =>
        (await mediator.Send(command, token)).ToActionResult();
}
