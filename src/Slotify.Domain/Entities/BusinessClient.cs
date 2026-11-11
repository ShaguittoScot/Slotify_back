namespace Slotify.Domain.Entities;

using Slotify.Domain.Common;

/// <summary>
/// Ficha de un cliente dentro de la cartera de un negocio específico (CRM Multi-Inquilino).
/// Mapea a la tabla: clientes_negocio
/// Relacionado con: US-026 (Construcción de módulo de clientes) y US-022 (Aislamiento por BusinessId).
/// Permite almacenar datos de contacto propios del negocio, clientes sin citas previas y notas internas
/// sin modificar la entidad global Client (clientes B2C) ni afectar a otros negocios.
/// </summary>
public class BusinessClient : AuditableEntity
{
    /// <summary>FK al negocio propietario de esta ficha (id_negocio).</summary>
    public Guid BusinessId { get; set; }

    /// <summary>FK opcional al cliente consumidor global B2C (id_cliente), solo referencia de lectura.</summary>
    public Guid? ClientId { get; set; }

    /// <summary>Nombre(s) del cliente en la cartera del negocio (nombre).</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Apellido(s) del cliente en la cartera del negocio (apellido).</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Nombre completo calculado.</summary>
    public string FullName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{FirstName} {LastName}".Trim();

    /// <summary>Correo electrónico de contacto en este negocio (correo, opcional).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Teléfono o WhatsApp de contacto en este negocio (telefono, opcional).</summary>
    public string? Phone { get; set; }

    /// <summary>Notas internas del negocio sobre el cliente (notas, opcional).</summary>
    public string? Notes { get; set; }

    public DateTime? ArchivedAt { get; set; }

    /// <summary>Citas históricas identificadas al archivar la ficha.</summary>
    public Guid[] HistoryAppointmentIds { get; set; } = [];

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Negocio propietario de la ficha.</summary>
    public Business Business { get; set; } = null!;

    /// <summary>Referencia opcional al cliente consumidor global.</summary>
    public Client? Client { get; set; }
}

