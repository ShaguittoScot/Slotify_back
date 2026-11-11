using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Calendar;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

public class CreateScheduleBlockHandler(
    IScheduleBlockRepository scheduleBlockRepository,
    ICurrentUserService currentUserService,
    CalendarAvailability availability,
    ICalendarDatabase calendarDatabase) : IRequestHandler<CreateScheduleBlockCommand, Result<long>>
{
    public async Task<Result<long>> Handle(CreateScheduleBlockCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.BusinessId is not { } businessId || businessId == Guid.Empty)
            return Result<long>.Fail("El usuario autenticado no tiene un negocio asignado.", ErrorType.Forbidden);

        return await calendarDatabase.WithBusinessLockAsync(businessId, async token =>
        {
            if (request.EmployeeId is { } employeeId &&
                !(await calendarDatabase.GetEmployeesAsync(businessId, token)).Any(e => e.Id == employeeId))
                return Result<long>.Fail("El empleado no está disponible para este negocio.", ErrorType.Validation);

            var reason = request.Reason?.Trim();
            if (await availability.IsOccupiedAsync(businessId, request.EmployeeId, request.StartDateTime, request.EndDateTime, token))
            {
                var blocks = await scheduleBlockRepository.GetByDateRangeAsync(
                    businessId, request.StartDateTime, request.EndDateTime, token);
                var existing = blocks.FirstOrDefault(b => b.EmployeeId == request.EmployeeId &&
                    b.StartDateTime == request.StartDateTime && b.EndDateTime == request.EndDateTime && b.Reason == reason);
                return existing is not null
                    ? Result<long>.Ok(existing.Id)
                    : Result<long>.Fail("El horario ya tiene una cita o un bloqueo.", ErrorType.Conflict);
            }

            var created = await scheduleBlockRepository.CreateAsync(new ScheduleBlock
            {
                BusinessId = businessId,
                EmployeeId = request.EmployeeId,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Reason = reason
            }, token);

            return Result<long>.Ok(created.Id, "Bloqueo de horario creado exitosamente.");
        }, cancellationToken);
    }
}
