namespace Slotify.Domain.Interfaces;

/// <summary>
/// Contrato CRUD genérico desacoplado del tipo de clave primaria (ISP / LSP / DIP).
/// 
/// NOTA DE ARQUITECTURA (SRP / Unit of Work):
/// Los repositorios que implementan este contrato NO llaman a SaveChangesAsync.
/// La confirmación transaccional es responsabilidad exclusiva de IUnitOfWork.
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
