namespace Slotify.Application.Common.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Contrato para generación y validación de tokens JWT.
/// Relacionado con: US-000 (Autenticación)
/// 
/// IMPLEMENTACIÓN PENDIENTE en Infrastructure/Services/JwtService.cs
/// Debe usar la configuración de appsettings.json → Jwt:Secret, Jwt:Issuer, etc.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera un access token JWT para el usuario autenticado.
    /// Claims mínimos: sub (userId), email, role, businessId.
    /// </summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <returns>Token JWT firmado como string.</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Genera un refresh token criptográficamente seguro.
    /// </summary>
    /// <returns>Refresh token como string opaco.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Valida un access token y extrae los claims.
    /// </summary>
    /// <param name="token">Token JWT a validar.</param>
    /// <returns>UserId extraído del token, o null si es inválido.</returns>
    Guid? ValidateAccessToken(string token);
}
