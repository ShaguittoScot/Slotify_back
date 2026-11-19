using Slotify.Application.Features.Calendar;
using Slotify.Application.Features.Calendar.DTOs;

namespace Slotify.Tests;

internal sealed class TestCalendarDatabase : ICalendarDatabase
{
    public Task<T> WithBusinessLockAsync<T>(
        Guid businessId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        operation(cancellationToken);

    public Task<IReadOnlyList<CalendarEmployeeDto>> GetEmployeesAsync(Guid businessId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CalendarEmployeeDto>>([]);
}
