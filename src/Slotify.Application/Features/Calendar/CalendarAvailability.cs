using Slotify.Application.Features.Calendar.DTOs;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Calendar;

public interface ICalendarDatabase
{
    Task<T> WithBusinessLockAsync<T>(
        Guid businessId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);

    Task<IReadOnlyList<CalendarEmployeeDto>> GetEmployeesAsync(Guid businessId, CancellationToken cancellationToken);
}

public sealed class CalendarAvailability(IAppointmentRepository appointments, IScheduleBlockRepository blocks)
{
    public async Task<bool> IsOccupiedAsync(
        Guid businessId, Guid? employeeId, DateTime start, DateTime end,
        CancellationToken cancellationToken, Guid? ignoreAppointmentId = null)
    {
        var overlappingAppointments = await appointments.GetByDateRangeAsync(businessId, start, end, cancellationToken);
        if (overlappingAppointments.Any(a => a.Id != ignoreAppointmentId &&
            a.Status != AppointmentStatus.Cancelled && SharesResource(employeeId, a.EmployeeId)))
        {
            return true;
        }

        var overlappingBlocks = await blocks.GetByDateRangeAsync(businessId, start, end, cancellationToken);
        return overlappingBlocks.Any(b => SharesResource(employeeId, b.EmployeeId));
    }

    private static bool SharesResource(Guid? first, Guid? second) =>
        first is null || second is null || first == second;
}
