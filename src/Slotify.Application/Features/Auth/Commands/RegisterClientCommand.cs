namespace Slotify.Application.Features.Auth.Commands;

using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;

/// <summary>
/// Comando para registrar o sincronizar un cliente consumidor (CLIENTE) en PostgreSQL.
/// </summary>
public record RegisterClientCommand : IRequest<Result<ClientDto>>
{
    /// <summary>ID generado por Supabase Auth (auth.users.id)</summary>
    public required Guid Id { get; init; }

    /// <summary>Nombre del cliente.</summary>
    public required string FirstName { get; init; }

    /// <summary>Apellido del cliente (opcional).</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>Nombre completo opcional para retrocompatibilidad.</summary>
    public string? FullName { get; init; }

    /// <summary>Correo electrónico único.</summary>
    public required string Email { get; init; }

    /// <summary>Teléfono o WhatsApp de contacto (opcional).</summary>
    public string? Phone { get; init; }
}
