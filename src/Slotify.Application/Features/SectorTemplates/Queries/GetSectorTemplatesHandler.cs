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
public class GetSectorTemplatesHandler(Slotify.Domain.Interfaces.ISectorTemplateRepository repository)
    : IRequestHandler<GetSectorTemplatesQuery, Result<List<SectorTemplateDto>>>
{
    private readonly Slotify.Domain.Interfaces.ISectorTemplateRepository _repository = repository;

    public async Task<Result<List<SectorTemplateDto>>> Handle(
        GetSectorTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _repository.GetAllAsync(cancellationToken);
        
        var dtos = templates.Select(t => new SectorTemplateDto
        {
            Id = t.Id,
            Name = t.Name,
            DefaultModules = System.Text.Json.JsonSerializer.Deserialize<List<string>>(t.DefaultModules) ?? [],
            SuggestedServices = System.Text.Json.JsonSerializer.Deserialize<List<SuggestedServiceDto>>(t.SuggestedServices) ?? [],
            FormConfig = System.Text.Json.JsonSerializer.Deserialize<BookingFormConfigDto>(t.FormConfig, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new BookingFormConfigDto()
        }).ToList();

        return Result<List<SectorTemplateDto>>.Ok(dtos);
    }
}
