using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Handler para crear un bloqueo de agenda.
/// Relacionado con: US-008 (Bloqueo Manual de Horarios No Disponibles)
/// </summary>
public class CreateScheduleBlockHandler(
    IScheduleBlockRepository scheduleBlockRepository,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<CreateScheduleBlockCommand, Result<long>>
{
    private readonly IScheduleBlockRepository _scheduleBlockRepository = scheduleBlockRepository;
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;

    public async Task<Result<long>> Handle(
        CreateScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar si existen citas confirmadas en el rango solicitado
        IReadOnlyList<Appointment> existingAppointments;
        if (request.EmployeeId.HasValue)
        {
            existingAppointments = await _appointmentRepository.GetByEmployeeAndDateRangeAsync(
                request.EmployeeId.Value,
                request.StartDateTime,
                request.EndDateTime,
                cancellationToken);
        }
        else
        {
            existingAppointments = await _appointmentRepository.GetByDateRangeAsync(
                request.BusinessId,
                request.StartDateTime,
                request.EndDateTime,
                cancellationToken);
        }

        var activeAppointments = existingAppointments
            .Where(a => a.Status == AppointmentStatus.Confirmed)
            .ToList();

        if (activeAppointments.Count > 0)
        {
            return Result<long>.Fail($"No se puede crear el bloqueo porque existen {activeAppointments.Count} cita(s) confirmada(s) en este horario.");
        }

        // 2. Crear entidad ScheduleBlock
        var scheduleBlock = new ScheduleBlock
        {
            BusinessId = request.BusinessId,
            EmployeeId = request.EmployeeId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Reason = request.Reason
        };

        // 3. Persistir en base de datos
        var created = await _scheduleBlockRepository.CreateAsync(scheduleBlock, cancellationToken);

        // 4. Retornar ID generado
        return Result<long>.Ok(created.Id, "Bloqueo de horario creado exitosamente.");
    }
}
