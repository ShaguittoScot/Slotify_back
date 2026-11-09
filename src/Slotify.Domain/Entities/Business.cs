namespace Slotify.Domain.Entities;

using System.Text.RegularExpressions;
using Slotify.Domain.Common;

/// <summary>
/// Negocio registrado en la plataforma.
/// Mapea a la tabla: negocios
/// Relacionado con: US-000 (se crea al registrar admin), US-001, US-005
/// </summary>
public partial class Business : AuditableEntity
{
    private static readonly Regex HexColorRegex = new(
        @"^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$",
        RegexOptions.Compiled);

    /// <summary>Slug único para URL pública (slug).</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Nombre del negocio (nombre).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto (telefono).</summary>
    public string? Phone { get; set; }

    /// <summary>Zona horaria del negocio (zona_horaria). Default: America/Mexico_City</summary>
    public string TimeZone { get; set; } = "America/Mexico_City";

    /// <summary>Horas mínimas de anticipación para agendar (anticipacion_minima_horas).</summary>
    public int MinAdvanceHours { get; set; } = 2;

    /// <summary>Días máximos de anticipación para agendar (anticipacion_maxima_dias).</summary>
    public int MaxAdvanceDays { get; set; } = 30;

    /// <summary>FK a la plantilla de sector seleccionada (id_plantilla_sector).</summary>
    public int? SectorTemplateId { get; set; }

    /// <summary>Configuración personalizada del formulario e identidad de marca en JSONB (configuracion_formulario_personalizada).</summary>
    public string? CustomFormConfig { get; set; }

    // ── Navegación ────────────────────────────────────────────────────────────
    /// <summary>Plantilla de sector (giro comercial) asociada.</summary>
    public SectorTemplate? SectorTemplate { get; set; }

    /// <summary>Usuarios del negocio.</summary>
    public ICollection<User> Users { get; set; } = [];

    /// <summary>Identidad de marca y personalización visual.</summary>
    public BusinessBrand? Brand { get; set; }

    /// <summary>Catálogo de servicios ofrecidos por el negocio.</summary>
    public ICollection<Service> Services { get; set; } = [];
}
