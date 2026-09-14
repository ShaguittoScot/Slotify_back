namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio especializado para negocios.
/// Relacionado con: US-000 (creación de negocio al registrar admin)
/// </summary>
public interface IBusinessRepository : IRepository<Business>
{
    /// <summary>
    /// Busca un negocio por su slug único.
    /// </summary>
    Task<Business?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un slug ya está en uso.
    /// Utilizado al crear un negocio para garantizar unicidad.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
}
