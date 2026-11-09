using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Application.Features.Businesses.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para administrar la configuración de Negocios.
/// Relacionado con: US-013 (Migración de plantilla), US-017/019 (Form config), US-022 (Protección por negocio del token).
/// </summary>
[ApiController]
[Route("api/businesses")]
[Authorize]
public class BusinessesController(
    IMediator mediator,
    ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// Cambia la plantilla de sector (giro) de un negocio existente (US-013).
    /// Devuelve 403 si el {id} no pertenece al negocio del token autenticado (US-022).
    /// </summary>
    [HttpPut("{id}/template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTemplate(
        Guid id,
        [FromBody] ChangeTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!BelongsToAuthenticatedBusiness(id))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = "No tienes permisos para modificar otro negocio." });
        }

        try
        {
            var result = await _mediator.Send(new ChangeBusinessTemplateCommand(id, request.NewTemplateId), cancellationToken);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Consulta pública de la configuración del formulario de reservas por slug.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{slug}/booking-config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingConfig(string slug, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBusinessBookingConfigQuery(slug), cancellationToken);
        if (!result.Success)
            return NotFound(new { error = result.Errors.FirstOrDefault() });

        return Ok(result.Data);
    }

    /// <summary>
    /// Actualiza la configuración del formulario de reservas del negocio.
    /// Devuelve 403 si el {id} no pertenece al negocio del token autenticado (US-022).
    /// </summary>
    [HttpPut("{id}/booking-config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBookingConfig(
        Guid id,
        [FromBody] UpdateFormConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (!BelongsToAuthenticatedBusiness(id))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { error = "No tienes permisos para modificar otro negocio." });
        }

        var jsonString = JsonSerializer.Serialize(request.FormConfig);
        var command = new UpdateBusinessFormConfigCommand(id, jsonString);
        var success = await _mediator.Send(command, cancellationToken);

        if (!success)
            return BadRequest(new { error = "No se pudo actualizar la configuración." });

        return Ok(new { message = "Configuración actualizada exitosamente." });
    }

    private bool BelongsToAuthenticatedBusiness(Guid routeBusinessId) =>
        routeBusinessId != Guid.Empty
        && _currentUserService.BusinessId is { } tokenBusinessId
        && tokenBusinessId != Guid.Empty
        && tokenBusinessId == routeBusinessId;
}

public class UpdateFormConfigRequest
{
    public object FormConfig { get; set; } = new();
}

public class ChangeTemplateRequest
{
    public int NewTemplateId { get; set; }
}
