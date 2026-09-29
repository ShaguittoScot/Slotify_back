namespace Slotify.Domain.Entities;

using Slotify.Domain.Common;

/// <summary>
/// Cliente consumidor de la plataforma (B2C / CRM Multi-negocio).
/// Mapea a la tabla: clientes
/// </summary>
public class Client : AuditableEntity
{
    /// <summary>ID generado por Supabase Auth (auth.users.id).</summary>
    public Guid? SupabaseId { get; set; }

    /// <summary>Nombre del cliente (nombre).</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Apellido del cliente (apellido).</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Nombre completo calculado.</summary>
    public string FullName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{FirstName} {LastName}".Trim();

    /// <summary>Correo electrónico único (correo).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Teléfono o WhatsApp del cliente (telefono).</summary>
    public string? Phone { get; set; }

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Citas agendadas por este cliente.</summary>
    public ICollection<Appointment> Appointments { get; set; } = [];
}
