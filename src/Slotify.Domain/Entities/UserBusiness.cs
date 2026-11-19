namespace Slotify.Domain.Entities;

using Slotify.Domain.Common;
using Slotify.Domain.Enums;

/// <summary>
/// Representa la vinculación de un usuario con un negocio.
/// Mapea a la tabla física: usuarios_negocios
/// Permite que una cuenta de usuario administre múltiples establecimientos físicos.
/// </summary>
public class UserBusiness : BaseEntity
{
    /// <summary>FK al usuario (id_usuario).</summary>
    public Guid UserId { get; set; }

    /// <summary>FK al negocio (id_negocio).</summary>
    public Guid BusinessId { get; set; }

    /// <summary>Rol del usuario en este negocio (rol): DUENO o EMPLEADO.</summary>
    public UserRole Role { get; set; } = UserRole.Owner;

    /// <summary>Indica si la vinculación está activa (esta_activo).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Fecha de creación de la vinculación (fecha_creacion).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Usuario vinculado.</summary>
    public User User { get; set; } = null!;

    /// <summary>Negocio vinculado.</summary>
    public Business Business { get; set; } = null!;
}
