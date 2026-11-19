namespace Slotify.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

/// <summary>
/// Implementación EF Core para IBusinessClientRepository con aislamiento estricto por BusinessId.
/// </summary>
public class BusinessClientRepository(AppDbContext context)
    : Repository<BusinessClient>(context), IBusinessClientRepository
{
    public async Task<IReadOnlyList<BusinessClientOption>> SearchActiveAsync(
        Guid businessId, string search, CancellationToken cancellationToken = default)
    {
        var term = search.ToLowerInvariant();
        return await _dbSet.AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.ArchivedAt == null &&
                ((c.FirstName + " " + c.LastName).ToLower().Contains(term) ||
                 c.Email.ToLower().Contains(term) ||
                 (c.Phone != null && c.Phone.Contains(term))))
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ThenBy(c => c.Id)
            .Take(8)
            .Select(c => new BusinessClientOption(c.Id, (c.FirstName + " " + c.LastName).Trim(), c.Phone, c.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessClient>> GetByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return [];

        return await _dbSet
            .Where(c => c.BusinessId == businessId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<BusinessClient?> GetByIdAndBusinessIdAsync(
        Guid id,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || businessId == Guid.Empty)
            return null;

        return await _dbSet.Where(c => c.BusinessId == businessId && (c.Id == id || c.ClientId == id))
            .OrderByDescending(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

