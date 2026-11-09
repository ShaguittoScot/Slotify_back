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

/// <summary>
/// DTO de configuración del formulario público de reservas e identidad de marca corporativa.
/// </summary>
public record BookingFormConfigDto
{
    public string Title { get; init; } = "Nueva Reservación";
    public string Description { get; init; } = string.Empty;
    public string PresentationStyle { get; init; } = "classic_scroll";

    /// <summary>Color corporativo primario (#RRGGBB).</summary>
    public string AccentColor { get; init; } = "#6366F1";

    /// <summary>Color corporativo secundario (#RRGGBB).</summary>
    public string SecondaryColor { get; init; } = "#8B5CF6";

    /// <summary>URL o Data URI del logotipo del establecimiento.</summary>
    public string? LogoUrl { get; init; }

    /// <summary>Nombre comercial del establecimiento mostrado en la interfaz pública.</summary>
    public string? BusinessName { get; init; }

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
