using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Infrastructure.Data;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para administrar y consultar Negocios.
/// </summary>
[ApiController]
[Route(api/businesses)]
public class BusinessesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Obtiene la lista de todos los negocios registrados para el explorador de clientes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromServices] AppDbContext context,
        CancellationToken cancellationToken)
    {
        var businesses = await context.Businesses
            .Include(b => b.SectorTemplate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = businesses.Select(b => new
        {
            id = b.Id.ToString(),
            name = b.Name,
            slug = !string.IsNullOrWhiteSpace(b.Slug) ? b.Slug : b.Name.ToLower().Replace( , -),
            phone = b.Phone,
            category = b.SectorTemplate?.Name ?? Servicios Generales,
            categoryKey = b.SectorTemplate?.Name?.ToLower() ?? general,
            timeZone = b.TimeZone,
            minAdvanceHours = b.MinAdvanceHours,
            maxAdvanceDays = b.MaxAdvanceDays,
            sectorTemplateId = b.SectorTemplateId
        });

        return Ok(result);
    }

    /// <summary>
    /// Cambia la plantilla de sector (giro) de un negocio existente (US-013).
    /// </summary>
    [HttpPut({id}/template)]
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

    [HttpGet({slug}/booking-config)]
    public async Task<IActionResult> GetBookingConfig(string slug, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new Slotify.Application.Features.Businesses.Queries.GetBusinessBookingConfigQuery(slug), cancellationToken);
        if (!result.Success)
            return NotFound(new { error = result.Errors.FirstOrDefault() });

        return Ok(result.Data);
    }

    [HttpPut({id}/booking-config)]
    public async Task<IActionResult> UpdateBookingConfig(Guid id, [FromBody] UpdateFormConfigRequest request, CancellationToken cancellationToken)
    {
        var jsonString = System.Text.Json.JsonSerializer.Serialize(request.FormConfig);
        var command = new Slotify.Application.Features.Businesses.Commands.UpdateBusinessFormConfigCommand(id, jsonString);
        var success = await _mediator.Send(command, cancellationToken);
        
        if (!success)
            return BadRequest(new { error = No se pudo actualizar la configuración. });

        return Ok(new { message = Configuración actualizada exitosamente. });
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
