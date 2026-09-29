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
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string PresentationStyle { get; init; } = "classic_scroll";
    public string AccentColor { get; init; } = "#6366F1";
    public List<FormFieldDto> Fields { get; init; } = [];
}

public record FormFieldDto
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Question { get; init; } = string.Empty;
    public bool Required { get; init; }
    public List<string>? Options { get; init; }
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
