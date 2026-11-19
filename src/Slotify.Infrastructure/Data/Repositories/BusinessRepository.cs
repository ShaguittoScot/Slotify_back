using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

public class BusinessRepository(AppDbContext context) : Repository<Business>(context), IBusinessRepository
{
    public override async Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Brand)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public override async Task UpdateAsync(Business entity, CancellationToken cancellationToken = default)
    {
        if (entity.Brand is not null && _context.Entry(entity.Brand).State == EntityState.Detached)
        {
            var exists = await _context.BusinessBrands
                .AsNoTracking()
                .AnyAsync(b => b.BusinessId == entity.Id, cancellationToken);

            if (exists)
                _context.BusinessBrands.Update(entity.Brand);
            else
                await _context.BusinessBrands.AddAsync(entity.Brand, cancellationToken);
        }

        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Update(entity);
        }
    }

    public async Task<Business?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(b => b.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetAllWithDetailsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.OnboardingCompletedAt != null)
            .Include(b => b.SectorTemplate)
            .Include(b => b.Brand)
            .Include(b => b.Services.Where(s => s.IsActive && s.DeletedAt == null))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Business?> GetBySlugWithDetailsAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim();
        return await _dbSet
            .Where(b => b.Slug == normalized && b.OnboardingCompletedAt != null)
            .Include(b => b.SectorTemplate)
            .Include(b => b.Brand)
            .Include(b => b.Services.Where(s => s.IsActive && s.DeletedAt == null))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Business?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // 1. Relación N:M usuarios_negocios (membresía activa como propietario)
        var businessFromMembership = await _context.UserBusinesses
            .Where(ub => ub.UserId == userId && ub.Role == UserRole.Owner && ub.IsActive)
            .Select(ub => ub.Business)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessFromMembership is not null)
            return businessFromMembership;

        // 2. Retrocompatibilidad: relación directa legacy en usuarios.id_negocio
        return await _context.Users
            .Where(u => u.Id == userId && u.Role == UserRole.Owner)
            .Select(u => u.Business)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
