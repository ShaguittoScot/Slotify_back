using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;

namespace Slotify.Application.Features.Auth.Commands;

/// <summary>
/// Comando para iniciar sesión como administrador.
/// Relacionado con: US-000 (Autenticación de Administrador)
/// 
/// Flujo:
/// 1. Buscar usuario por email
/// 2. Verificar contraseña con BCrypt
/// 3. Verificar que esté activo
/// 4. Actualizar último inicio de sesión
/// 5. Generar tokens JWT
/// 6. Guardar sesión con refresh token
/// 7. Retornar AuthResponse
/// </summary>
public record LoginCommand : IRequest<Result<AuthResponse>>
{
    /// <summary>Correo electrónico del usuario.</summary>
    public required string Email { get; init; }

    /// <summary>Contraseña en texto plano.</summary>
    public required string Password { get; init; }
}
