namespace Slotify.Domain.Entities;

using Slotify.Domain.Common;
using Slotify.Domain.Enums;

/// <summary>
/// Cita/reserva agendada con un cliente.
/// Mapea a la tabla: citas
/// Relacionado con: US-003 (Navegación en Calendario — muestra citas)
/// 
/// NOTA: La lógica de validación de empalmes se maneja en la BD
/// con la restricción de exclusión (EXCLUDE USING gist).
/// </summary>
public class Appointment : AuditableEntity
{
    /// <summary>FK al negocio (id_negocio).</summary>
    public Guid BusinessId { get; set; }

    /// <summary>FK al empleado asignado, nullable (id_empleado).</summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>Inicio de la cita (hora_inicio).</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Fin de la cita (hora_fin).</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Nombre del cliente (nombre_cliente).</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>Correo del cliente (correo_cliente).</summary>
    public string ClientEmail { get; set; } = string.Empty;

    /// <summary>Teléfono del cliente (telefono_cliente).</summary>
    public string ClientPhone { get; set; } = string.Empty;

    /// <summary>Estado actual de la cita (estado).</summary>
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;

    /// <summary>Token único para cancelación por el cliente (token_cancelacion).</summary>
    public string CancellationToken { get; set; } = string.Empty;

    /// <summary>Total pactado de la cita (total_pactado).</summary>
    public decimal AgreedTotal { get; set; } = 0.00m;

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Negocio donde se agendó la cita.</summary>
    public Business Business { get; set; } = null!;
}
