using System.Data;
using Microsoft.EntityFrameworkCore;
using Slotify.Application.Features.Calendar;
using Slotify.Application.Features.Calendar.DTOs;

namespace Slotify.Infrastructure.Data;

public sealed class CalendarDatabase(AppDbContext context) : ICalendarDatabase
{
    public Task<T> WithBusinessLockAsync<T>(
        Guid businessId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async token =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
            try
            {
                var lockKey = $"slotify:calendar:{businessId:D}";
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", token);
                var result = await operation(token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch
            {
                context.ChangeTracker.Clear();
                throw;
            }
        }, cancellationToken);

    public async Task<IReadOnlyList<CalendarEmployeeDto>> GetEmployeesAsync(Guid businessId, CancellationToken cancellationToken) =>
        await context.Database.SqlQuery<CalendarEmployeeDto>($"""
            SELECT e.id AS "Id", concat_ws(' ', u.nombre, NULLIF(u.apellido, '')) AS "Name"
            FROM public.empleados e
            JOIN public.usuarios u ON u.id = e.id_usuario AND u.id_negocio = e.id_negocio
            WHERE e.id_negocio = {businessId} AND e.esta_activo AND u.esta_activo
            ORDER BY "Name", e.id
            """).ToListAsync(cancellationToken);
}
