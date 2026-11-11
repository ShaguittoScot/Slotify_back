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
                     && (a.EmployeeId == null || a.EmployeeId == employeeId)
                     && a.StartTime < endDate
                     && a.EndTime > startDate)
            .OrderBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return [];

        var appointments = await _dbSet
            .Include(a => a.Client)
            .Where(a => a.BusinessId == businessId)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync(cancellationToken);

        if (appointments.Count == 0) return appointments;
        var titles = await _context.Database.SqlQuery<ServiceTitleRow>($"""
            SELECT cs.id_cita AS "Id",
                string_agg(DISTINCT s.nombre::text, ' + ' ORDER BY s.nombre::text) AS "Title"
            FROM public.citas_servicios cs
            JOIN public.citas a ON a.id = cs.id_cita
            JOIN public.servicios s ON s.id = cs.id_servicio AND s.id_negocio = a.id_negocio
            WHERE a.id_negocio = {businessId}
            GROUP BY cs.id_cita
            """).ToDictionaryAsync(row => row.Id, row => row.Title, cancellationToken);

        foreach (var appointment in appointments)
            appointment.ServiceTitle = titles.GetValueOrDefault(appointment.Id);

        return appointments;
    }

    private sealed record ServiceTitleRow(Guid Id, string Title);
}
