using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

/// <summary>
/// Implementación EF Core para IScheduleBlockRepository.
/// Relacionado con: US-008 (Bloqueo Manual de Horarios No Disponibles)
/// </summary>
public class ScheduleBlockRepository(AppDbContext context) : IScheduleBlockRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IReadOnlyList<ScheduleBlock>> GetByDateRangeAsync(
        Guid businessId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.ScheduleBlocks
            .Where(b => b.BusinessId == businessId
                     && b.StartDateTime < endDate
                     && b.EndDateTime > startDate)
            .OrderBy(b => b.StartDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduleBlock> CreateAsync(
        ScheduleBlock block,
        CancellationToken cancellationToken = default)
    {
        await _context.ScheduleBlocks.AddAsync(block, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return block;
    }

    public async Task<ScheduleBlock?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ScheduleBlocks
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task DeleteAsync(
        ScheduleBlock block,
        CancellationToken cancellationToken = default)
    {
        _context.ScheduleBlocks.Remove(block);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
