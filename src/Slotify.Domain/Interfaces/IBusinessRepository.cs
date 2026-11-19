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

    /// <summary>
    /// Obtiene todos los negocios con sus entidades relacionadas (plantilla de sector, identidad visual de marca y catálogo de servicios).
    /// </summary>
    Task<IReadOnlyList<Business>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un negocio por su slug único con sus entidades relacionadas (plantilla de sector, marca y catálogo de servicios activos),
    /// únicamente si ha completado el proceso de onboarding comercial.
    /// </summary>
    Task<Business?> GetBySlugWithDetailsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el negocio asociado al usuario propietario consultando la relación en base de datos.
    /// </summary>
    Task<Business?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
