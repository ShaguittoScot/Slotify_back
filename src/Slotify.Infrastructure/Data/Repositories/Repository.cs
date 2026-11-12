using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Common;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository implementation using EF Core.
/// 
/// NOTA DE ARQUITECTURA (SRP / Unit of Work):
/// Este repositorio NO llama a SaveChangesAsync.
/// El commit de la transacción es responsabilidad exclusiva de IUnitOfWork.
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
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
