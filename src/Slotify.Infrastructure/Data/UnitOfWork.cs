using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly AppDbContext _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
