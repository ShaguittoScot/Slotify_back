using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

/// <summary>
/// Implementación EF Core para IScheduleBlockRepository.
/// Relacionado con: US-008 (Bloqueo Manual de Horarios No Disponibles), US-022 (Aislamiento por negocio) y US-023.
/// 
/// NOTA DE ARQUITECTURA (SRP / Unit of Work):
/// Este repositorio NO llama a SaveChangesAsync. La persistencia transaccional es responsabilidad
/// exclusiva de IUnitOfWork, garantizando coherencia con el resto de repositorios de la arquitectura.
/// </summary>
public class ScheduleBlockRepository(AppDbContext context) : IScheduleBlockRepository
{
    private readonly AppDbContext _context = context;

    public async Task<ScheduleBlock?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ScheduleBlocks
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleBlock>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ScheduleBlocks
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduleBlock> AddAsync(
        ScheduleBlock entity,
        CancellationToken cancellationToken = default)
    {
        await _context.ScheduleBlocks.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(
        ScheduleBlock entity,
        CancellationToken cancellationToken = default)
    {
        _context.ScheduleBlocks.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ScheduleBlock entity,
        CancellationToken cancellationToken = default)
    {
        _context.ScheduleBlocks.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ScheduleBlock>> GetByDateRangeAsync(
        Guid businessId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return [];

        return await _context.ScheduleBlocks
            .Where(b => b.BusinessId == businessId
                     && b.StartDateTime < endDate
                     && b.EndDateTime > startDate)
            .OrderBy(b => b.StartDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleBlock>> GetByEmployeeAndDateRangeAsync(
        Guid businessId,
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return [];

        return await _context.ScheduleBlocks
            .Where(b => b.BusinessId == businessId
                     && (!b.EmployeeId.HasValue || b.EmployeeId == employeeId)
                     && b.StartDateTime < endDate
                     && b.EndDateTime > startDate)
            .OrderBy(b => b.StartDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasConflictAsync(
        Guid businessId,
        Guid? employeeId,
        DateTime startDateTime,
        DateTime endDateTime,
        long? excludeBlockId = null,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return false;

        var query = _context.ScheduleBlocks
            .Where(b => b.BusinessId == businessId);

        if (excludeBlockId.HasValue)
            query = query.Where(b => b.Id != excludeBlockId.Value);

        if (employeeId.HasValue)
            query = query.Where(b => !b.EmployeeId.HasValue || b.EmployeeId == employeeId.Value);

        return await query.AnyAsync(
            b => b.StartDateTime < endDateTime && b.EndDateTime > startDateTime,
            cancellationToken);
    }
}
