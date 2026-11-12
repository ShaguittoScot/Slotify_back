using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.Commands;
using Slotify.API.Services;
using System.Net.Http.Headers;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller de autenticación para administradores de negocio y emisión de JWT.
/// Relacionado con: US-000 (Registro y Autenticación) y US-022 (Token con claim businessId).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(IMediator mediator, ISupabaseIdentityVerifier supabase) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Intercambia un token de sesión verificado de Supabase por un JWT de Slotify.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExchangeToken(
        CancellationToken cancellationToken) =>
        await VerifyAndExecuteAsync(
            cancellationToken,
            identity => identity is not null,
            async identity =>
            {
                var result = await _mediator.Send(new ExchangeTokenCommand(identity!.Id, identity.Email), cancellationToken);
                return result.ErrorType switch
                {
                    ErrorType.NotFound => NotFound(new { success = false, code = "business_profile_missing", error = result.Errors[0] }),
                    _ => result.ToActionResult()
                };
            });

    /// <summary>
    /// Registra un nuevo administrador y crea su negocio, devolviendo el perfil y su JWT.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Sync(
        [FromBody] RegisterAdminCommand command,
        CancellationToken cancellationToken) =>
        await VerifyAndExecuteAsync(
            cancellationToken,
            identity => Matches(identity, command.Id, command.Email),
            async _ => (await _mediator.Send(command, cancellationToken)).ToActionResult());

    /// <summary>
    /// Registra o sincroniza un cliente consumidor (CLIENTE) en la tabla clientes.
    /// </summary>
    [HttpPost("sync-client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SyncClient(
        [FromBody] RegisterClientCommand command,
        CancellationToken cancellationToken) =>
        await VerifyAndExecuteAsync(
            cancellationToken,
            identity => Matches(identity, command.Id, command.Email),
            async _ => (await _mediator.Send(command, cancellationToken)).ToActionResult());

    private async Task<IActionResult> VerifyAndExecuteAsync(
        CancellationToken cancellationToken,
        Func<SupabaseIdentity?, bool> predicate,
        Func<SupabaseIdentity?, Task<IActionResult>> action)
    {
        var identity = await GetVerifiedIdentity(cancellationToken);
        return predicate(identity) switch
        {
            true => await action(identity),
            false => InvalidSession()
        };
    }

    private async Task<SupabaseIdentity?> GetVerifiedIdentity(CancellationToken cancellationToken)
    {
        var hasValidBearer =
            AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) &&
            string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(header.Parameter);

        return hasValidBearer switch
        {
            true => await supabase.VerifyAsync(header!.Parameter!, cancellationToken),
            false => null
        };
    }

    private static bool Matches(SupabaseIdentity? identity, Guid id, string? email) =>
        identity is not null && identity.Id == id &&
        string.Equals(identity.Email, email?.Trim(), StringComparison.OrdinalIgnoreCase);

    private IActionResult InvalidSession() =>
        Unauthorized(new { success = false, error = "Inicia sesión con una cuenta válida de Supabase." });
}
