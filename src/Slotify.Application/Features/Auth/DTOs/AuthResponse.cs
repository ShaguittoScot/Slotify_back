namespace Slotify.Application.Features.Auth.DTOs;

/// <summary>
/// Respuesta de autenticación devuelta al cliente.
/// Relacionado con: US-000 (Login y Register retornan tokens)
/// </summary>
public record AuthResponse
{
    /// <summary>Token JWT de acceso (corta duración).</summary>
    public required string AccessToken { get; init; }

    /// <summary>Token de refresco (larga duración, opaco).</summary>
    public required string RefreshToken { get; init; }

    /// <summary>Fecha de expiración del access token (UTC ISO 8601).</summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>Datos básicos del usuario autenticado.</summary>
    public required AuthUserDto User { get; init; }
}
