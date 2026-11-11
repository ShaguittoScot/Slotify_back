namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

public sealed record BusinessClientOption(Guid Id, string FullName, string? Phone, string Email);

/// <summary>
/// Repositorio para la cartera de clientes aislada por negocio (tabla clientes_negocio).
/// Relacionado con: US-026 (Construcción de módulo de clientes) y US-022 (Aislamiento por BusinessId).
/// </summary>
public interface IBusinessClientRepository : IRepository<BusinessClient>
{
    Task<IReadOnlyList<BusinessClientOption>> SearchActiveAsync(
        Guid businessId, string search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todas las fichas de clientes registradas en un negocio específico.
    /// </summary>
    Task<IReadOnlyList<BusinessClient>> GetByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una ficha de cliente por su ID (o ClientId vinculado) verificando pertenencia estricta al negocio.
    /// </summary>
    Task<BusinessClient?> GetByIdAndBusinessIdAsync(
        Guid id,
        Guid businessId,
        CancellationToken cancellationToken = default);
}

