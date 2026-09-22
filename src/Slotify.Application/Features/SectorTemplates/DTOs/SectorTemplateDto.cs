namespace Slotify.Application.Features.SectorTemplates.DTOs;

/// <summary>
/// DTO para plantilla de sector / giro comercial.
/// Relacionado con: US-006 (Selector de Giro), US-002 (Plantillas por Sector)
/// </summary>
public record SectorTemplateDto
{
    /// <summary>ID de la plantilla.</summary>
    public required int Id { get; init; }

    /// <summary>Nombre del giro/sector. Ej: "Barbería"</summary>
    public required string Name { get; init; }

    /// <summary>Módulos activados por defecto (deserializado del JSONB).</summary>
    public required List<string> DefaultModules { get; init; }

    /// <summary>Servicios sugeridos (deserializado del JSONB).</summary>
    public required List<SuggestedServiceDto> SuggestedServices { get; init; }

    /// <summary>Configuración del formulario (deserializado del JSONB).</summary>
    public required BookingFormConfigDto FormConfig { get; init; }
}

public record BookingFormConfigDto
{
    public bool RequiresProfessional { get; init; }
    public bool RequiresService { get; init; }
    public bool RequiresGuestCount { get; init; }
    public bool RequiresTable { get; init; }
    public bool RequiresPatientDetails { get; init; }
}

/// <summary>
/// DTO para un servicio sugerido dentro de una plantilla.
/// </summary>
public record SuggestedServiceDto
{
    public required string Name { get; init; }
    public required int DurationMinutes { get; init; }
    public decimal? Price { get; init; }
}
