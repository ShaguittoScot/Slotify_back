using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Application.Features.Businesses.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para administrar la configuración e identidad corporativa de Negocios.
/// </summary>
[ApiController]
[Route("api/businesses")]
[Authorize]
public class BusinessesController(
    IMediator mediator,
    ICurrentUserService currentUserService) : ControllerBase
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// Obtiene la lista de negocios para el explorador y catálogo de clientes.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<BusinessListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBusinessesQuery(),
            cancellationToken);

        return Ok(result);
    }

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
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "No tiene autorización para modificar este negocio." });

        try
        {
            var result = await _mediator.Send(
                new ChangeBusinessTemplateCommand(id, request.NewTemplateId),
                cancellationToken);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene la configuración de reserva de un negocio por su slug (público).
    /// </summary>
    [HttpGet("{slug}/booking-config")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingConfig(string slug, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBusinessBookingConfigQuery(slug),
            cancellationToken);

        if (!result.Success)
            return NotFound(new { error = result.Errors.FirstOrDefault() });

        return result.ToActionResult(r => Ok(r.Data));
    }

    /// <summary>
    /// Actualiza la configuración de formulario personalizada de un negocio.
    /// </summary>
    [HttpPut("{id}/booking-config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBookingConfig(
        Guid id,
        [FromBody] UpdateFormConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (!BelongsToAuthenticatedBusiness(id))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "No tiene autorización para modificar este negocio." });

        var jsonString = JsonSerializer.Serialize(request.FormConfig, CamelCaseOptions);
        var command = new UpdateBusinessFormConfigCommand(id, jsonString);
        var result = await _mediator.Send(command, cancellationToken);

        return result.ToActionResult(_ =>
            Ok(new { message = "Configuración e identidad de marca actualizadas exitosamente." }));
    }

    /// <summary>
    /// Carga el logotipo y define los colores corporativos principales del negocio sobre su formulario de reservas.
    /// </summary>
    [HttpPut("{id}/branding")]
    [HttpPatch("{id}/branding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBranding(
        Guid id,
        [FromBody] UpdateBusinessBrandingRequest request,
        CancellationToken cancellationToken)
    {
        if (!BelongsToAuthenticatedBusiness(id))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "No tiene autorización para modificar este negocio." });

        var command = new UpdateBusinessBrandIdentityCommand(
            id,
            request.LogoUrl,
            request.AccentColor,
            request.SecondaryColor,
            request.BusinessName);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    private bool BelongsToAuthenticatedBusiness(Guid routeBusinessId) =>
        routeBusinessId != Guid.Empty
        && _currentUserService.BusinessId is { } tokenBusinessId
        && tokenBusinessId != Guid.Empty
        && tokenBusinessId == routeBusinessId;
}

public class UpdateBusinessBrandingRequest
{
    public string? LogoUrl { get; set; }
    public string AccentColor { get; set; } = "#6366F1";
    public string? SecondaryColor { get; set; }
    public string? BusinessName { get; set; }
}
