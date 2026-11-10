using Microsoft.EntityFrameworkCore;
using Slotify.Application.Common.Interfaces;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data;

public class BusinessOnboardingPersistence(AppDbContext context) : IBusinessOnboardingPersistence
{
    private readonly AppDbContext _context = context;

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default
    )
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                cancellationToken
            );
            try
            {
                await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<User?> GetUserWithBusinessDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .Users.Include(u => u.Business)
                .ThenInclude(b => b.Brand)
            .Include(u => u.Business)
                .ThenInclude(b => b.Services)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<bool> HasBusinessHoursAsync(
        Guid businessId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.AvailabilitySchedules.AnyAsync(
            schedule => schedule.BusinessId == businessId && schedule.EmployeeId == null,
            cancellationToken
        );
    }

    public void AddBusiness(Business business)
    {
        _context.Businesses.Add(business);
    }

    public void AddUser(User user)
    {
        _context.Users.Add(user);
    }

    public void AddBusinessBrand(BusinessBrand brand)
    {
        _context.BusinessBrands.Add(brand);
    }

    public void AddServices(IEnumerable<Service> services)
    {
        _context.Services.AddRange(services);
    }

    public void AddBusinessHours(IEnumerable<AvailabilitySchedule> schedules)
    {
        _context.AvailabilitySchedules.AddRange(schedules);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
