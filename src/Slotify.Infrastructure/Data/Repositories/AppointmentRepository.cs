using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

/// <summary>
/// Implementación EF Core para IAppointmentRepository.
/// Relacionado con: US-022 (Aislamiento estricto por BusinessId) y US-023.
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

    public async Task<bool> HasActiveAppointmentConflictAsync(
        Guid businessId,
        Guid? employeeId,
        DateTime startDate,
        DateTime endDate,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return false;

        var query = _dbSet.Where(a => a.BusinessId == businessId
                                   && a.Status == AppointmentStatus.Confirmed);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        if (employeeId.HasValue)
            query = query.Where(a => !a.EmployeeId.HasValue || a.EmployeeId == employeeId.Value);

        return await query.AnyAsync(
            a => a.StartTime < endDate && a.EndTime > startDate,
            cancellationToken);
    }
}
