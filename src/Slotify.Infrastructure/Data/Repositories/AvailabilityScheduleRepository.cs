using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

public sealed class AvailabilityScheduleRepository(AppDbContext context) : IAvailabilityScheduleRepository
{
    public async Task<IReadOnlyList<AvailabilitySchedule>> GetByBusinessIdAsync(Guid businessId, CancellationToken token = default) =>
        await context.AvailabilitySchedules.AsNoTracking().Where(x => x.BusinessId == businessId).ToListAsync(token);

    public async Task<IReadOnlyList<AvailabilitySchedule>> GetByBusinessAndDayAsync(
        Guid businessId, short dayOfWeek, CancellationToken token = default) =>
        await context.AvailabilitySchedules.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.DayOfWeek == dayOfWeek).ToListAsync(token);
}
