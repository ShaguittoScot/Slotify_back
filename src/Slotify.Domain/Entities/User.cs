namespace Slotify.Domain.Entities;

using Slotify.Domain.Common;
using Slotify.Domain.Enums;

/// <summary>
/// Usuario del sistema (dueño o empleado de un negocio).
/// Mapea a la tabla: usuarios
/// Relacionado con: US-000 (Registro y Autenticación de Administrador)
/// </summary>
public class User : AuditableEntity
{
    /// <summary>Negocio al que pertenece el usuario (id_negocio).</summary>
    public Guid BusinessId { get; set; }

    /// <summary>Nombre completo del usuario (nombre_completo).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Correo electrónico único (correo).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash de contraseña (obsoleto con Supabase Auth pero requerido por DB schema viejo).</summary>
    public string PasswordHash { get; set; } = "managed_by_supabase";


    /// <summary>Rol del usuario: DUENO o EMPLEADO (rol).</summary>
    public UserRole Role { get; set; } = UserRole.Owner;

    /// <summary>Si el usuario está activo (esta_activo).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Si el correo ha sido verificado (correo_verificado).</summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>Fecha del último inicio de sesión (ultimo_inicio_sesion).</summary>
    public DateTime? LastLogin { get; set; }

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Negocio al que pertenece.</summary>
    public Business Business { get; set; } = null!;

}
