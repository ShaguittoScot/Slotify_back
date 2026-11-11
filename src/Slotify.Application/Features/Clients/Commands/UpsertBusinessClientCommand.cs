using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Clients.DTOs;

namespace Slotify.Application.Features.Clients.Commands;

/// <summary>
/// Comando CQRS para registrar o actualizar los datos de contacto y notas de un cliente en la cartera del negocio.
/// Relacionado con: US-026 (Construcción de módulo de clientes).
/// </summary>
public record UpsertBusinessClientCommand : IRequest<Result<BusinessClientDto>>
{
    public Guid? ClientId { get; init; }
    public bool IsExplicitUpdateById { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
