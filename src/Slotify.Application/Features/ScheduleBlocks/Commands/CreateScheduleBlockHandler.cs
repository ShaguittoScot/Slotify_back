using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Handler para crear un bloqueo de agenda.
/// Cumple con SRP, DIP y Unit of Work delegando la persistencia a IUnitOfWork.
/// Relacionado con: US-008, US-022 y US-023.
/// </summary>
public class CreateScheduleBlockHandler(
    IScheduleBlockRepository scheduleBlockRepository,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateScheduleBlockCommand, Result<long>>
{
    private readonly IScheduleBlockRepository _scheduleBlockRepository = scheduleBlockRepository;
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<long>> Handle(
        CreateScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return Result<long>.Fail("El usuario autenticado no tiene un negocio asignado.");
        }

        // 1. Verificar si existen citas confirmadas en el rango solicitado
        var hasAppointmentConflict = await _appointmentRepository.HasActiveAppointmentConflictAsync(
            businessId,
            request.EmployeeId,
            request.StartDateTime,
            request.EndDateTime,
            null,
            cancellationToken);

        if (hasAppointmentConflict)
        {
            return Result<long>.Fail("No se puede crear el bloqueo porque existen citas confirmadas en este horario.");
        }

        // 2. Verificar si ya existe otro bloqueo solapado en la misma franja
        var hasBlockConflict = await _scheduleBlockRepository.HasConflictAsync(
            businessId,
            request.EmployeeId,
            request.StartDateTime,
            request.EndDateTime,
            null,
            cancellationToken);

        if (hasBlockConflict)
        {
            return Result<long>.Fail("Ya existe un bloqueo de horario en la franja seleccionada.");
        }

        // 3. Crear entidad ScheduleBlock
        var scheduleBlock = new ScheduleBlock
        {
            BusinessId = businessId,
            EmployeeId = request.EmployeeId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Reason = !string.IsNullOrWhiteSpace(request.Reason) ? request.Reason.Trim() : "Horario Bloqueado"
        };

        // 4. Persistir a través del repositorio y confirmar vía Unit of Work (SRP)
        await _scheduleBlockRepository.AddAsync(scheduleBlock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Retornar ID generado
        return Result<long>.Ok(scheduleBlock.Id, "Bloqueo de horario creado exitosamente.");
    }
}
