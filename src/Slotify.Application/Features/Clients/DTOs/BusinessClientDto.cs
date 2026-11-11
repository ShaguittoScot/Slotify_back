namespace Slotify.Application.Features.Clients.DTOs;

/// <summary>
/// Resumen e historial de un cliente dentro de la cartera de un negocio específico.
/// Relacionado con: US-026 (Construcción de módulo de clientes).
/// </summary>
public class BusinessClientDto
{
    /// <summary>Identificador único de la ficha en la cartera del negocio.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID para editar la ficha. Si solo hay citas de un cliente global, contiene su ID hasta que se registre la ficha.</summary>
    public Guid? ClientId { get; set; }

    public bool IsRegistered { get; set; }

    public DateTime? ArchivedAt { get; set; }

    /// <summary>Nombre completo del cliente.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Nombre(s) del cliente.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Apellido(s) del cliente.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Correo electrónico de contacto real (sin placeholders ni correos sintéticos).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Teléfono o WhatsApp de contacto real.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Notas internas del negocio sobre el cliente.</summary>
    public string? Notes { get; set; }

    /// <summary>Total de visitas válidas (confirmadas o completadas) en este negocio.</summary>
    public int TotalVisits { get; set; }

    /// <summary>Total de citas completadas en este negocio.</summary>
    public int CompletedVisits { get; set; }

    /// <summary>Total de citas canceladas o inasistencias (no-show) en este negocio.</summary>
    public int CancelledVisits { get; set; }

    /// <summary>Suma de los importes pactados de las citas completadas en este negocio.</summary>
    public decimal TotalSpent { get; set; }

    /// <summary>Fecha de la última cita válida registrada (pasada o actual).</summary>
    public DateTime? LastVisitAt { get; set; }

    /// <summary>Fecha de la próxima cita confirmada futura, si existe.</summary>
    public DateTime? NextVisitAt { get; set; }

    /// <summary>Fecha del primer registro o primera cita con el negocio.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Historial cronológico de citas del cliente con este negocio.</summary>
    public IReadOnlyList<ClientAppointmentItemDto> Appointments { get; set; } = [];
}

/// <summary>
/// Detalle individual de una cita en el historial de un cliente.
/// </summary>
public class ClientAppointmentItemDto
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "confirmed";
    public string? Title { get; set; }
    public decimal AgreedTotal { get; set; }
    public Guid? EmployeeId { get; set; }
}
