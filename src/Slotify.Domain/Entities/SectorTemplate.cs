namespace Slotify.Domain.Entities;

/// <summary>
/// Plantilla base por sector/giro comercial.
/// Mapea a la tabla: plantillas_sector
/// Relacionado con: US-002 (Implementación de Plantillas por Sector),
///                  US-006 (Selector Inicial de Giro Comercial)
/// 
/// NOTA: Usa PK int (SERIAL) — NO hereda de BaseEntity (que usa Guid).
/// Los datos se pre-cargan via seed data.
/// </summary>
public class SectorTemplate
{
    /// <summary>Identificador autoincremental (id SERIAL).</summary>
    public int Id { get; set; }

    /// <summary>Nombre del giro/sector (nombre). Ej: "Barbería", "Consultorio Dental"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Módulos activados por defecto para este sector (modulos_defecto).
    /// Almacenado como JSONB. Ej: ["citas", "servicios", "empleados"]
    /// </summary>
    public string DefaultModules { get; set; } = "[]";

    /// <summary>
    /// Servicios sugeridos para el sector (servicios_sugeridos).
    /// Almacenado como JSONB. Ej: [{"nombre": "Corte", "duracion": 30}]
    /// </summary>
    public string SuggestedServices { get; set; } = "[]";

    /// <summary>
    /// Configuración dinámica del formulario de reservas en formato JSON.
    /// Dictamina qué campos requiere este giro (ej. seleccionar empleado, número de personas, etc.)
    /// </summary>
    public string FormConfig { get; set; } = "{}";

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Negocios que usan esta plantilla.</summary>
    public ICollection<Business> Businesses { get; set; } = [];
}
