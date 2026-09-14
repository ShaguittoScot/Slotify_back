namespace Slotify.Application.Features.Auth.DTOs;

/// <summary>
/// Datos del usuario incluidos en la respuesta de autenticación.
/// </summary>
public record AuthUserDto
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public required Guid BusinessId { get; init; }
}
