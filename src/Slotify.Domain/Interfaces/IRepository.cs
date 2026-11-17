namespace Slotify.Domain.Interfaces;

/// <summary>
/// Contrato CRUD genérico desacoplado del tipo de clave primaria (ISP / LSP / DIP).
/// </summary>
public interface IRepository<T, in TId> : IReadRepository<T, TId> where T : class
{
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Variante CRUD estándar para entidades con clave primaria tipo Guid.
/// </summary>
public interface IRepository<T> : IRepository<T, Guid> where T : class
{
}
