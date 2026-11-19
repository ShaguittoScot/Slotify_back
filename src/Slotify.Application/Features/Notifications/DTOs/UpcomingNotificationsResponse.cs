namespace Slotify.Application.Features.Notifications.DTOs;

/// <summary>
/// DTO de una cita próxima para el motor de notificaciones push.
/// US-Notifications: Motor de notificaciones push para negocio y cliente.
/// </summary>
public record UpcomingAppointmentDto
{
    /// <summary>ID único de la cita.</summary>
    public required string Id { get; init; }

    /// <summary>Inicio de la cita en UTC ISO 8601.</summary>
    public required DateTime StartTime { get; init; }

    /// <summary>Fin de la cita en UTC ISO 8601.</summary>
    public required DateTime EndTime { get; init; }

    /// <summary>Nombre del cliente.</summary>
    public string? ClientName { get; init; }

    /// <summary>Estado de la cita: confirmed, cancelled, completed, no_show.</summary>
    public required string Status { get; init; }

    /// <summary>Nombre del empleado asignado (si aplica).</summary>
    public string? EmployeeName { get; init; }

    /// <summary>Nombre del negocio (para clientes que ven sus citas).</summary>
    public string? BusinessName { get; init; }

    /// <summary>Total acordado.</summary>
    public decimal? AgreedTotal { get; init; }
}

/// <summary>
/// Respuesta del endpoint de notificaciones próximas.
/// Incluye un hash de snapshot para que el cliente detecte cambios sin comparar elemento a elemento.
/// </summary>
public record UpcomingNotificationsResponse
{
    /// <summary>Citas próximas dentro del horizonte solicitado.</summary>
    public required List<UpcomingAppointmentDto> Appointments { get; init; }

    /// <summary>
    /// Hash SHA-256 (hex) del snapshot actual de citas: ID+Status+StartTime.
    /// Si el hash cambia entre dos llamadas, hay una modificación en la agenda.
    /// </summary>
    public required string SnapshotHash { get; init; }

    /// <summary>Timestamp UTC en que se generó la respuesta.</summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>Horizonte de días consultados desde ahora.</summary>
    public int HorizonDays { get; init; }
}
