using System.Net.Http.Headers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.API.Services;
using Slotify.Application.Features.PublicBooking;

namespace Slotify.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/consumer/appointments")]
public sealed class ConsumerAppointmentsController(
    IMediator mediator,
    ISupabaseIdentityVerifier supabase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOwnAppointments(CancellationToken cancellationToken)
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return Unauthorized(new { success = false, error = "Inicia sesión para consultar tus citas." });
        }

        var identity = await supabase.VerifyAsync(header.Parameter, cancellationToken);
        if (identity is null)
            return Unauthorized(new { success = false, error = "Tu sesión de Supabase no es válida o expiró." });

        return (await mediator.Send(new GetConsumerAppointmentsQuery(identity.Id, identity.Email), cancellationToken))
            .ToActionResult();
    }
}
