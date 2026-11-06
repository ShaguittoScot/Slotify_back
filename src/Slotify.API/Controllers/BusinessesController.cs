using MediatR;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Application.Features.Businesses.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para administrar y consultar Negocios.
/// Cumple con Single Responsibility (SRP) e Inversión de Dependencias (DIP) delegando a Casos de Uso vía MediatR.
/// </summary>
[ApiController]
[Route("api/businesses")]
public class BusinessesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Obtiene la lista de negocios para el explorador y catálogo de clientes.
    /// </summary>
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
    /// </summary>
    [HttpPut("{id}/template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTemplate(
        Guid id,
        [FromBody] ChangeTemplateRequest request,
        CancellationToken cancellationToken)
    {
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
    /// Obtiene la configuración de reserva de un negocio por su slug.
    /// </summary>
    [HttpGet("{slug}/booking-config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingConfig(string slug, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBusinessBookingConfigQuery(slug),
            cancellationToken);

        if (!result.Success)
            return NotFound(new { error = result.Errors.FirstOrDefault() });

        return Ok(result.Data);
    }

    /// <summary>
    /// Actualiza la configuración de formulario personalizada de un negocio.
    /// </summary>
    [HttpPut("{id}/booking-config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBookingConfig(
        Guid id,
        [FromBody] UpdateFormConfigRequest request,
        CancellationToken cancellationToken)
    {
        var jsonString = System.Text.Json.JsonSerializer.Serialize(request.FormConfig);
        var command = new UpdateBusinessFormConfigCommand(id, jsonString);
        var success = await _mediator.Send(command, cancellationToken);

        if (!success)
            return BadRequest(new { error = "No se pudo actualizar la configuración." });

        return Ok(new { message = "Configuración actualizada exitosamente." });
    }
}
