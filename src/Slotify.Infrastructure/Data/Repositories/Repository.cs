using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Common;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository implementation using EF Core.
/// 
/// NOTA: Este repositorio NO llama a SaveChangesAsync.
/// El commit de la transacción es responsabilidad exclusiva de IUnitOfWork.
/// Llamar a SaveChanges aquí violaría SRP y el patrón Unit of Work.
/// </summary>
public class Repository<T>(AppDbContext context) : IRepository<T>
    where T : BaseEntity
{
    protected readonly AppDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        // No SaveChanges aquí — el IUnitOfWork del handler controla el commit.
        return entity;
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        // No SaveChanges aquí — el IUnitOfWork del handler controla el commit.
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        // No SaveChanges aquí — el IUnitOfWork del handler controla el commit.
    }
}
