using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Calendar.DTOs;

namespace Slotify.Application.Features.Calendar.Queries;

public record GetCalendarEmployeesQuery : IRequest<Result<IReadOnlyList<CalendarEmployeeDto>>>;

public sealed class GetCalendarEmployeesHandler(ICalendarDatabase calendarDatabase, ICurrentUserService currentUser)
    : IRequestHandler<GetCalendarEmployeesQuery, Result<IReadOnlyList<CalendarEmployeeDto>>>
{
    public async Task<Result<IReadOnlyList<CalendarEmployeeDto>>> Handle(
        GetCalendarEmployeesQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.BusinessId is not { } businessId || businessId == Guid.Empty)
            return Result<IReadOnlyList<CalendarEmployeeDto>>.Fail("El usuario no tiene un negocio asignado.", ErrorType.Forbidden);

        return Result<IReadOnlyList<CalendarEmployeeDto>>.Ok(
            await calendarDatabase.GetEmployeesAsync(businessId, cancellationToken));
    }
}
