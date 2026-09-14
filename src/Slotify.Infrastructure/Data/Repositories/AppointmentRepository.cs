using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

/// <summary>
/// Implementación EF Core para IAppointmentRepository.
/// </summary>
public class AppointmentRepository(AppDbContext context) : Repository<Appointment>(context), IAppointmentRepository
{
    public async Task<IReadOnlyList<Appointment>> GetByDateRangeAsync(
        Guid businessId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.BusinessId == businessId
                     && a.StartTime < endDate
                     && a.EndTime > startDate)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByEmployeeAndDateRangeAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.EmployeeId == employeeId
                     && a.StartTime < endDate
                     && a.EndTime > startDate)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }
}
