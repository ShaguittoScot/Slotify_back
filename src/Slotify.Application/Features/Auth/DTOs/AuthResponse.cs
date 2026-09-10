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

/// <summary>
/// Datos del usuario incluidos en la respuesta de auth.
/// Subconjunto seguro — NO incluye passwordHash.
/// </summary>
public record AuthUserDto
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public required Guid BusinessId { get; init; }
}
