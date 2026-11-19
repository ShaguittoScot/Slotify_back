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
[Route("api/public/businesses")]
public sealed class PublicBookingController(
    IMediator mediator,
    ISupabaseIdentityVerifier supabase) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBusiness(string slug, CancellationToken cancellationToken) =>
        (await mediator.Send(new GetPublicBusinessBookingQuery(slug), cancellationToken)).ToActionResult();

    [HttpGet("{slug}/availability")]
    public async Task<IActionResult> GetAvailability(
        string slug,
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
            return BadRequest(new { success = false, error = "Indica una fecha válida con formato YYYY-MM-DD." });

        return (await mediator.Send(new GetPublicAvailabilityQuery(slug, parsedDate), cancellationToken))
            .ToActionResult();
    }

    [HttpPost("{slug}/appointments")]
    public async Task<IActionResult> CreateAppointment(
        string slug,
        [FromBody] CreatePublicAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        Guid? clientId = null;
        string? email = command.ClientEmail;

        if (AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) &&
            string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(header.Parameter))
        {
            try
            {
                var identity = await supabase.VerifyAsync(header.Parameter, cancellationToken);
                if (identity is not null)
                {
                    clientId = identity.Id;
                    if (string.IsNullOrWhiteSpace(email))
                        email = identity.Email;
                }
            }
            catch
            {
                // Las reservas públicas también permiten invitados sin sesión válida.
            }
        }

        var normalized = command with { Slug = slug, ClientId = clientId, ClientEmail = email };
        return (await mediator.Send(normalized, cancellationToken)).ToActionResult();
    }
}
