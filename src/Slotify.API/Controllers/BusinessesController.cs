using MediatR;
using Microsoft.AspNetCore.Mvc;
using Slotify.API.Common;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Application.Features.Businesses.Queries;
using System.Text.Json;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para administrar la configuración e identidad corporativa de Negocios.
/// </summary>
[ApiController]
[Route("api/businesses")]
public class BusinessesController(IMediator mediator) : ControllerBase
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Cambia la plantilla de sector (giro) de un negocio existente (US-013).
    /// </summary>
    [HttpPut("{id}/template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    /// </summary>
    [HttpPut("{id}/booking-config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// </summary>
    [HttpPut("{id}/branding")]
    [HttpPatch("{id}/branding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

public class UpdateFormConfigRequest
{
    public object FormConfig { get; set; } = new();
}

public class UpdateBusinessBrandingRequest
{
    public string? LogoUrl { get; set; }
    public string AccentColor { get; set; } = "#6366F1";
    public string? SecondaryColor { get; set; }
    public string? BusinessName { get; set; }
}

public class ChangeTemplateRequest
{
    public int NewTemplateId { get; set; }
}
