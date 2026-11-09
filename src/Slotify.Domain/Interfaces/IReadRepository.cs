namespace Slotify.Domain.Interfaces;

/// <summary>
/// Contrato para repositorios de solo lectura.
/// Usar cuando la entidad no necesita operaciones de escritura
/// (ej: catálogos, seed data, entidades inmutables).
/// 
/// Beneficio ISP: los consumers no dependen de métodos (Add/Update/Delete)
/// que nunca van a usar.
/// 
/// Relacionado con: ISectorTemplateRepository (seed data de giros comerciales)
/// </summary>
public interface IReadRepository<T> where T : class
{
    /// <summary>Obtiene una entidad por su identificador.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene todas las entidades disponibles.</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
}
