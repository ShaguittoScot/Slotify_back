namespace Slotify.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing transactions.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
