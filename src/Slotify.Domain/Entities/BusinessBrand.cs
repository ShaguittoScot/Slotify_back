namespace Slotify.Domain.Entities;

/// <summary>
/// Marca e identidad visual de un negocio (marca_negocio).
/// Relación 1:1 con Business.
/// </summary>
public class BusinessBrand
{
    /// <summary>FK y PK al negocio (id_negocio).</summary>
    public Guid BusinessId { get; set; }

    /// <summary>URL del logotipo alojado (url_logotipo).</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Color primario en formato hexadecimal (color_primario). Ej: "#6366F1"</summary>
    public string? PrimaryColor { get; set; } = "#000000";

    /// <summary>Indica si se utilizan los valores predeterminados (es_predeterminado).</summary>
    public bool IsDefault { get; set; } = true;

    /// <summary>Fecha de última actualización (fecha_actualizacion).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Negocio propietario de la marca.</summary>
    public Business Business { get; set; } = null!;
}
