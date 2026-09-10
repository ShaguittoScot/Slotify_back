using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

public class BusinessRepository(AppDbContext context) : Repository<Business>(context), IBusinessRepository
{
    public async Task<Business?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(b => b.Slug == slug, cancellationToken);
    }
}
