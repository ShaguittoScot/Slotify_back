using MediatR;
using Microsoft.AspNetCore.Mvc;
using Slotify.Application.Features.SectorTemplates.Queries;

namespace Slotify.API.Controllers;

/// <summary>
/// Controller para plantillas de sector / giros comerciales.
/// Relacionado con: US-006 (Selector Inicial de Giro Comercial),
///                  US-002 (Implementación de Plantillas por Sector)
/// 
/// Endpoints:
/// - GET /api/sector-templates → Listar todas las plantillas (seed data)
/// 
/// NOTA: Este endpoint es público ya que se usa durante el onboarding
/// antes de que el admin tenga un token JWT.
/// </summary>
[ApiController]
[Route("api/sector-templates")]
public class SectorTemplatesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Obtiene todas las plantillas de sector disponibles.
    /// </summary>
    /// <remarks>
    /// Retorna la lista pre-cargada de giros comerciales con sus
    /// módulos por defecto y servicios sugeridos.
    /// Usado por el frontend en el paso de onboarding (US-006).
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSectorTemplatesQuery(), cancellationToken);
        return Ok(result);
    }
}
