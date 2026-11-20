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
        RegexOptions.Compiled
    );

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

    /// <summary>Indica la fecha y hora en que el negocio completó el proceso de onboarding (onboarding_completed_at).</summary>
    public DateTime? OnboardingCompletedAt { get; set; }

    /// <summary>Descripción comercial del establecimiento (descripcion).</summary>
    public string? Description { get; set; }

    // ── Ubicación Física (Establecimiento Único) ───────────────

    /// <summary>Calle de la ubicación física (calle).</summary>
    public string? Street { get; set; }

    /// <summary>Número exterior del establecimiento (numero_exterior).</summary>
    public string? ExteriorNumber { get; set; }

    /// <summary>Número interior del establecimiento (numero_interior).</summary>
    public string? InteriorNumber { get; set; }

    /// <summary>Colonia o barrio del establecimiento (colonia).</summary>
    public string? Neighborhood { get; set; }

    /// <summary>Ciudad o municipio del establecimiento (ciudad).</summary>
    public string? City { get; set; }

    /// <summary>Estado o provincia del establecimiento (estado).</summary>
    public string? State { get; set; }

    /// <summary>Código postal del establecimiento (codigo_postal).</summary>
    public string? PostalCode { get; set; }

    /// <summary>Código ISO de país (codigo_pais). Default: "MX"</summary>
    public string? CountryCode { get; set; } = "MX";

    // ── Navegación ────────────────────────────────────────────────────────────
    /// <summary>Plantilla de sector (giro comercial) asociada.</summary>
    public SectorTemplate? SectorTemplate { get; set; }

    /// <summary>Usuarios del negocio (relación legacy preservada para compatibilidad).</summary>
    public ICollection<User> Users { get; set; } = [];

    /// <summary>Vinculaciones del negocio con usuarios (relación N:M).</summary>
    public ICollection<UserBusiness> UserBusinesses { get; set; } = [];

    /// <summary>Identidad de marca y personalización visual.</summary>
    public BusinessBrand? Brand { get; set; }

    /// <summary>Catálogo de servicios ofrecidos por el negocio.</summary>
    public ICollection<Service> Services { get; set; } = [];

    // ── Lógica de Dominio ─────────────────────────────────────────────────────

    /// <summary>
    /// Segmentos de ruta reservados que no pueden ser utilizados como slug de un negocio
    /// para evitar colisiones con endpoints del sistema o recursos de la plataforma.
    /// </summary>
    public static readonly IReadOnlySet<string> ReservedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "me",
        "admin",
        "api",
        "book",
        "public",
        "settings",
        "auth",
        "health",
        "consumer",
        "notifications",
        "telemetry"
    };

    /// <summary>
    /// Determina si un slug coincide con un identificador o ruta reservada de la plataforma.
    /// </summary>
    public static bool IsReservedSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) && ReservedSlugs.Contains(slug.Trim().ToLowerInvariant());

    /// <summary>
    /// Valida si un slug es válido (no vacío y no reservado por el sistema).
    /// </summary>
    public static bool IsValidSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) && !IsReservedSlug(slug);

    /// <summary>
    /// Genera el slug URL-friendly a partir del nombre del negocio.
    /// Regla: minúsculas, sin espacios iniciales/finales, espacios internos reemplazados por guiones.
    /// Centralizado aquí (SRP) para que cualquier cambio de regla
    /// solo requiera editar esta clase.
    /// </summary>
    /// <param name="name">Nombre del negocio.</param>
    /// <returns>Slug generado. Ejemplo: "Mi Barbería" → "mi-barbería"</returns>
    public static string GenerateSlug(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim().ToLowerInvariant().Replace(" ", "-");

    /// <summary>
    /// Valida que un color corporativo cumpla con el formato hexadecimal #RGB o #RRGGBB.
    /// </summary>
    public static bool IsValidCorporateHexColor(string? color) =>
        !string.IsNullOrWhiteSpace(color) && HexColorRegex.IsMatch(color.Trim());

    /// <summary>
    /// Valida que la fuente del logotipo sea una URL HTTP(S) válida o un Data URI de imagen seguro.
    /// </summary>
    public static bool IsValidLogoSource(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
            return true; // El logotipo es opcional (puede removerse)

        var trimmed = logoUrl.Trim();
        if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return trimmed.Length <= 5_500_000; // Máximo ~4MB en Base64

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
