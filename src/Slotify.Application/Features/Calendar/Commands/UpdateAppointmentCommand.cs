using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Calendar.Commands;

public record UpdateAppointmentCommand(Guid Id, string? Status, DateTime? StartTime, DateTime? EndTime)
    : IRequest<Result>;

public sealed class UpdateAppointmentHandler(
    IAppointmentRepository appointments,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    CalendarAvailability availability,
    ICalendarDatabase calendarDatabase) : IRequestHandler<UpdateAppointmentCommand, Result>
{
    public async Task<Result> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.BusinessId is not { } businessId || businessId == Guid.Empty)
            return Result.Fail("El usuario no tiene un negocio asignado.", ErrorType.Forbidden);

        var changesTime = request.StartTime.HasValue || request.EndTime.HasValue;
        var status = request.Status switch
        {
            "completed" => AppointmentStatus.Completed,
            "cancelled" => AppointmentStatus.Cancelled,
            _ => (AppointmentStatus?)null
        };
        if ((changesTime && (request.Status is not null ||
            request.StartTime is not { Kind: DateTimeKind.Utc } start ||
            request.EndTime is not { Kind: DateTimeKind.Utc } end || start >= end)) ||
            (!changesTime && status is null))
        {
            return Result.Fail("Envía un estado válido o un rango de horas válido en UTC.", ErrorType.Validation);
        }

        return await calendarDatabase.WithBusinessLockAsync(businessId, async token =>
        {
            var appointment = await appointments.GetByIdAsync(request.Id, token);
            if (appointment is null || appointment.BusinessId != businessId)
                return Result.NotFound("No se encontró la cita.");

            if (appointment.Status == status ||
                (changesTime && appointment.StartTime == request.StartTime && appointment.EndTime == request.EndTime))
                return Result.Ok();

            if (appointment.Status != AppointmentStatus.Confirmed)
                return Result.Fail("Solo puedes modificar una cita confirmada.", ErrorType.Conflict);

            if (changesTime)
            {
                var newStart = request.StartTime!.Value;
                var newEnd = request.EndTime!.Value;
                if (await availability.IsOccupiedAsync(businessId, appointment.EmployeeId, newStart, newEnd, token, appointment.Id))
                    return Result.Fail("El horario ya tiene una cita o un bloqueo.", ErrorType.Conflict);

                appointment.StartTime = newStart;
                appointment.EndTime = newEnd;
            }
            else
            {
                appointment.Status = status!.Value;
            }

            await appointments.UpdateAsync(appointment, token);
            await unitOfWork.SaveChangesAsync(token);
            return Result.Ok();
        }, cancellationToken);
    }
}
