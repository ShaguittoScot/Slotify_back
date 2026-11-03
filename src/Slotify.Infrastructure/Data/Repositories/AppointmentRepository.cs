using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

/// <summary>
/// Implementación EF Core para IAppointmentRepository.
/// Relacionado con: US-022 (Aislamiento estricto por BusinessId).
/// </summary>
public class AppointmentRepository(AppDbContext context) : Repository<Appointment>(context), IAppointmentRepository
{
    public async Task<IReadOnlyList<Appointment>> GetByDateRangeAsync(
        Guid businessId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return [];

        return await _dbSet
            .Where(a => a.BusinessId == businessId
                     && a.StartTime < endDate
                     && a.EndTime > startDate)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByEmployeeAndDateRangeAsync(
        Guid businessId,
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return [];

        return await _dbSet
            .Where(a => a.BusinessId == businessId
                     && a.EmployeeId == employeeId
                     && a.StartTime < endDate
                     && a.EndTime > startDate)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }
}
