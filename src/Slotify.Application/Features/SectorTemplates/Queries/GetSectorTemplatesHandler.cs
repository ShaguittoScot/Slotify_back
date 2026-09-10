using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.SectorTemplates.DTOs;

namespace Slotify.Application.Features.SectorTemplates.Queries;

/// <summary>
/// Handler para obtener las plantillas de sector.
/// Relacionado con: US-006
/// 
/// DEPENDENCIAS REQUERIDAS:
/// - ISectorTemplateRepository: obtener plantillas
/// 
/// TODO: Implementar lógica + mapeo a DTO (deserializar JSONB).
/// </summary>
public class GetSectorTemplatesHandler
    : IRequestHandler<GetSectorTemplatesQuery, Result<List<SectorTemplateDto>>>
{
    // TODO: Inyectar ISectorTemplateRepository via constructor

    public async Task<Result<List<SectorTemplateDto>>> Handle(
        GetSectorTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar:
        // 1. Obtener todas las plantillas → ISectorTemplateRepository.GetAllAsync()
        // 2. Mapear cada SectorTemplate a SectorTemplateDto
        //    - Deserializar DefaultModules (JSONB string → List<string>)
        //    - Deserializar SuggestedServices (JSONB string → List<SuggestedServiceDto>)
        // 3. Retornar Result.Ok(dtos)

        throw new NotImplementedException(
            "US-006: Pendiente implementar GetSectorTemplatesHandler.");
    }
}
