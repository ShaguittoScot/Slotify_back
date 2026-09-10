namespace Slotify.Domain.Entities;

/// <summary>
/// Bloqueo manual de un rango de tiempo en la agenda.
/// Mapea a la tabla: bloqueos_agenda
/// Relacionado con: US-008 (Bloqueo Manual de Horarios No Disponibles)
/// 
/// NOTA: Usa PK long (BIGSERIAL) — NO hereda de BaseEntity.
/// </summary>
public class ScheduleBlock
{
    /// <summary>Identificador autoincremental (id BIGSERIAL).</summary>
    public long Id { get; set; }

    /// <summary>FK al negocio (id_negocio).</summary>
    public Guid BusinessId { get; set; }

    /// <summary>FK al empleado, nullable si aplica a todo el negocio (id_empleado).</summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>Inicio del bloqueo (fecha_hora_inicio).</summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>Fin del bloqueo (fecha_hora_fin).</summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>Motivo del bloqueo, opcional (motivo).</summary>
    public string? Reason { get; set; }

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Negocio al que pertenece el bloqueo.</summary>
    public Business Business { get; set; } = null!;
}
