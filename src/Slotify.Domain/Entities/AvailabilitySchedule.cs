namespace Slotify.Domain.Entities;

/// <summary>
/// Horario recurrente de disponibilidad (por día de la semana).
/// Mapea a la tabla: horarios_disponibilidad
/// Relacionado con: US-003 (Navegación en Calendario — muestra disponibilidad)
/// 
/// NOTA: Usa PK long (BIGSERIAL) — NO hereda de BaseEntity.
/// </summary>
public class AvailabilitySchedule
{
    /// <summary>Identificador autoincremental (id BIGSERIAL).</summary>
    public long Id { get; set; }

    /// <summary>FK al negocio (id_negocio).</summary>
    public Guid BusinessId { get; set; }

    /// <summary>FK al empleado, nullable si aplica a todo el negocio (id_empleado).</summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>Día de la semana 0=Domingo..6=Sábado (dia_semana).</summary>
    public short DayOfWeek { get; set; }

    /// <summary>Hora de inicio de disponibilidad (hora_inicio).</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>Hora de fin de disponibilidad (hora_fin).</summary>
    public TimeOnly EndTime { get; set; }

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Negocio al que pertenece el horario.</summary>
    public Business Business { get; set; } = null!;
}
