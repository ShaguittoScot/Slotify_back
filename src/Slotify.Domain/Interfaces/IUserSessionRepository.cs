namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para sesiones de usuario (refresh tokens).
/// Relacionado con: US-000 (manejo de sesiones JWT)
/// 
/// NOTA: No hereda de IRepository&lt;T&gt; porque UserSession usa Guid PK
/// pero tiene lógica de invalidación específica.
/// </summary>
public interface IUserSessionRepository
{
    /// <summary>
    /// Crea una nueva sesión con el hash del refresh token.
    /// </summary>
    Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca una sesión por el hash del refresh token.
    /// Usado para validar refresh tokens en el flujo de renovación.
    /// </summary>
    Task<UserSession?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoca todas las sesiones de un usuario (logout global).
    /// </summary>
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
