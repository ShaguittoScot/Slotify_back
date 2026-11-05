namespace Slotify.Domain.Entities;

using Slotify.Domain.Common;

/// <summary>
/// Servicio ofrecido por un negocio (servicios).
/// Relación N:1 con Business.
/// </summary>
public class Service : BaseEntity
{
    /// <summary>FK al negocio propietario (id_negocio).</summary>
    public Guid BusinessId { get; set; }

    /// <summary>Nombre del servicio (nombre).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Duración del servicio en minutos (duracion_minutos).</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Tiempo de colchón/limpieza entre citas en minutos (tiempo_colchon_minutos).</summary>
    public int BufferTimeMinutes { get; set; } = 0;

    /// <summary>Precio del servicio (precio).</summary>
    public decimal Price { get; set; } = 0.00m;

    /// <summary>Indica si el servicio está disponible para agendamiento (esta_activo).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Fecha de eliminación lógica (fecha_eliminacion), nullable.</summary>
    public DateTime? DeletedAt { get; set; }

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Negocio que ofrece este servicio.</summary>
    public Business Business { get; set; } = null!;
}
