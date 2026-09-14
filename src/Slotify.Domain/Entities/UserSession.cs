namespace Slotify.Domain.Entities;

using Slotify.Domain.Common;

/// <summary>
/// Sesión de usuario con token de refresco.
/// Mapea a la tabla: sesiones_usuario
/// Relacionado con: US-000 (Autenticación — manejo de refresh tokens)
/// </summary>
public class UserSession : BaseEntity
{
    /// <summary>FK al usuario dueño de la sesión (id_usuario).</summary>
    public Guid UserId { get; set; }

    /// <summary>Hash del token de refresco (token_refresco_hash). Nunca almacenar en texto plano.</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>Fecha de expiración del refresh token (fecha_expiracion).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Si la sesión fue revocada manualmente (revocado).</summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>Fecha de creación de la sesión (fecha_creacion).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navegación ──────────────────────────────────────────
    /// <summary>Usuario dueño de la sesión.</summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Marca la sesión como revocada.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
    }
}
