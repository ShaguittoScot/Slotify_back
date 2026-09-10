namespace Slotify.Domain.Enums;

/// <summary>
/// Estados posibles de una cita.
/// Mapea a: CHECK (estado IN ('confirmed', 'cancelled', 'completed', 'no_show'))
/// Relacionado con: US-003 (Calendario muestra citas por estado)
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Cita confirmada y vigente.</summary>
    Confirmed = 0,

    /// <summary>Cita cancelada por el cliente o admin.</summary>
    Cancelled = 1,

    /// <summary>Cita completada exitosamente.</summary>
    Completed = 2,

    /// <summary>Cliente no se presentó.</summary>
    NoShow = 3
}
