using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;

namespace Slotify.Application.Features.Auth.Commands;

/// <summary>
/// Comando para registrar un nuevo administrador de negocio (DUENO).
/// Relacionado con: US-000 (Registro de Administrador)
/// 
/// Flujo:
/// 1. Validar datos (RegisterAdminValidator)
/// 2. Verificar que el email no exista
/// 3. Crear el Business (negocio)
/// 4. Crear el User con rol DUENO
/// 5. Generar tokens JWT
/// 6. Retornar AuthResponse
/// </summary>
public record RegisterAdminCommand : IRequest<Result<AuthResponse>>
{
    /// <summary>Nombre completo del administrador.</summary>
    public required string FullName { get; init; }

    /// <summary>Correo electrónico (será el login).</summary>
    public required string Email { get; init; }

    /// <summary>Contraseña en texto plano (se hasheará con BCrypt).</summary>
    public required string Password { get; init; }

    /// <summary>Nombre del negocio a crear.</summary>
    public required string BusinessName { get; init; }

    /// <summary>Teléfono del negocio (opcional).</summary>
    public string? BusinessPhone { get; init; }

    /// <summary>ID de la plantilla de sector seleccionada en US-006 (opcional al registro).</summary>
    public int? SectorTemplateId { get; init; }
}
