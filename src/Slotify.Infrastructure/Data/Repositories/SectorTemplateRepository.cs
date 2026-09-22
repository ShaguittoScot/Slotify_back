using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

public class SectorTemplateRepository(AppDbContext context) : ISectorTemplateRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IReadOnlyList<SectorTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SectorTemplates
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<SectorTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SectorTemplates
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
