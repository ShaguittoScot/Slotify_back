using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;

namespace Slotify.Application.Features.Auth.Commands;

/// <summary>
/// Comando para registrar un nuevo administrador de negocio (DUENO).
/// Relacionado con: US-000 (Registro de Administrador)
/// 
/// Flujo (con Supabase Auth):
/// 1. Frontend registra al usuario en Supabase Auth
/// 2. Frontend llama a POST /api/auth/sync con el ID de Supabase
/// 3. Validar datos (RegisterAdminValidator)
/// 4. Verificar que el email no exista en nuestra BD
/// 5. Crear el Business (negocio)
/// 6. Crear el User con rol DUENO y el ID de Supabase
/// 7. Retornar AuthUserDto
/// </summary>
public record RegisterAdminCommand : IRequest<Result<AuthUserDto>>
{
    /// <summary>ID generado por Supabase Auth (auth.users.id)</summary>
    public required Guid Id { get; init; }

    /// <summary>Nombre completo del administrador.</summary>
    public required string FullName { get; init; }

    /// <summary>Correo electrónico (será el login).</summary>
    public required string Email { get; init; }

    /// <summary>Nombre del negocio a crear.</summary>
    public required string BusinessName { get; init; }

    /// <summary>Teléfono del negocio (opcional).</summary>
    public string? BusinessPhone { get; init; }

    /// <summary>ID de la plantilla de sector seleccionada en US-006 (opcional al registro).</summary>
    public int? SectorTemplateId { get; init; }
}
