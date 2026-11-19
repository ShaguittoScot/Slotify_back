using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Clients.DTOs;

namespace Slotify.Application.Features.Clients.Queries;

/// <summary>
/// Query para obtener la cartera de clientes e historial de citas del negocio autenticado.
/// Relacionado con: US-026 (Construcción de módulo de clientes) y US-022 (Aislamiento por BusinessId).
/// </summary>
public record GetBusinessClientsQuery : IRequest<Result<IReadOnlyList<BusinessClientDto>>>
{
    /// <summary>Texto opcional para filtrar clientes por nombre, correo o teléfono.</summary>
    public string? Search { get; init; }

    public bool IncludeArchived { get; init; }
}

