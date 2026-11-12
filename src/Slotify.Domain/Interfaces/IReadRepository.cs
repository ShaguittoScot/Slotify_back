namespace Slotify.Domain.Interfaces;

/// <summary>
/// Contrato base de solo lectura desacoplado del tipo de clave primaria (ISP).
/// Usar cuando la entidad no necesita operaciones de escritura
/// (ej: catálogos, seed data, entidades inmutables).
/// 
/// Beneficio ISP: los consumers no dependen de métodos (Add/Update/Delete)
/// que nunca van a usar.
/// </summary>
public interface IReadRepository<T, in TId> where T : class
{
    /// <summary>Obtiene una entidad por su identificador.</summary>
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene todas las entidades disponibles.</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Variante de solo lectura para entidades con identificador Guid estándar.
/// </summary>
public interface IReadRepository<T> : IReadRepository<T, Guid> where T : class
{
}
