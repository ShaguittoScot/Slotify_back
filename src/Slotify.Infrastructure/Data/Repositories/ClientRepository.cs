namespace Slotify.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

public class ClientRepository(AppDbContext context) : Repository<Client>(context), IClientRepository
{
    public async Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<Client?> GetBySupabaseIdAsync(Guid supabaseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.SupabaseId == supabaseId, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c => c.Email == email, cancellationToken);
    }
}
