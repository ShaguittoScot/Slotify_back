using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Application.Features.Businesses.Queries;
using System.Text.Json;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para administrar y consultar la configuración e identidad corporativa de Negocios.
/// Cumple con Single Responsibility (SRP) e Inversión de Dependencias (DIP) delegando a Casos de Uso vía MediatR.
/// </summary>
[ApiController]
[Route("api/businesses")]
[Authorize]
public class BusinessesController(IMediator mediator) : ControllerBase
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Obtiene la lista pública de negocios para el explorador y catálogo de clientes.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
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
        var result = await _mediator.Send(
            new ChangeBusinessTemplateCommand(id, request.NewTemplateId),
            cancellationToken);

        return result.ToActionResult(r => Ok(new { success = r.Data }));
    }

    /// <summary>
    /// Obtiene la configuración pública del formulario de reservas junto con el logotipo
    /// y los colores corporativos principales del negocio.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{slug}/booking-config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingConfig(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBusinessBookingConfigQuery(slug),
            cancellationToken);

        return result.ToActionResult(r => Ok(r.Data));
    }

    /// <summary>
    /// Actualiza la configuración del formulario de reservas e identidad de marca (logotipo y colores corporativos).
    /// Devuelve 403 si el {id} no pertenece al negocio del token autenticado (US-022).
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
        var jsonString = JsonSerializer.Serialize(request.FormConfig, CamelCaseOptions);
        var command = new UpdateBusinessFormConfigCommand(id, jsonString);
        var result = await _mediator.Send(command, cancellationToken);

        return result.ToActionResult(_ =>
            Ok(new { message = "Configuración e identidad de marca actualizadas exitosamente." }));
    }

    /// <summary>
    /// Carga el logotipo y define los colores corporativos principales del negocio sobre su formulario de reservas.
    /// Devuelve 403 si el {id} no pertenece al negocio del token autenticado (US-022).
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
        var command = new UpdateBusinessBrandIdentityCommand(
            id,
            request.LogoUrl,
            request.AccentColor,
            request.SecondaryColor,
            request.BusinessName);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}
