using MediatR;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.Businesses.Commands;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para administrar la configuración de Negocios.
/// </summary>
[ApiController]
[Route("api/businesses")]
public class BusinessesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Cambia la plantilla de sector (giro) de un negocio existente (US-013).
    /// </summary>
    [HttpPut("{id}/template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTemplate(Guid id, [FromBody] ChangeTemplateRequest request, CancellationToken cancellationToken)
    {
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
    [HttpGet("{slug}/booking-config")]
    public async Task<IActionResult> GetBookingConfig(string slug, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Slotify.Application.Features.Businesses.Queries.GetBusinessBookingConfigQuery(slug), cancellationToken);
        if (!result.Success)
            return NotFound(new { error = result.Errors.FirstOrDefault() });

        return Ok(result.Data);
    }

    [HttpPut("{id}/booking-config")]
    public async Task<IActionResult> UpdateBookingConfig(Guid id, [FromBody] UpdateFormConfigRequest request, CancellationToken cancellationToken)
    {
        var jsonString = System.Text.Json.JsonSerializer.Serialize(request.FormConfig);
        var command = new Slotify.Application.Features.Businesses.Commands.UpdateBusinessFormConfigCommand(id, jsonString);
        var success = await _mediator.Send(command, cancellationToken);
        
        if (!success)
            return BadRequest(new { error = "No se pudo actualizar la configuración." });

        return Ok(new { message = "Configuración actualizada exitosamente." });
    }
}

public class UpdateFormConfigRequest
{
    public object FormConfig { get; set; } = new();
}
public class ChangeTemplateRequest
{
    public int NewTemplateId { get; set; }
}
