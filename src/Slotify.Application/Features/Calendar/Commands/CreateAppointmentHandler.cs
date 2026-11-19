using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Calendar.Commands;

/// <summary>
/// Handler para orquestar la creación y persistencia de una nueva cita en el calendario.
/// Cumple con Single Responsibility (SRP) e Inversión de Dependencias (DIP), inyectando solo abstracciones.
/// Relacionado con: US-022 (BusinessId resuelto desde ICurrentUserService).
/// </summary>
public class CreateAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IBusinessRepository businessRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    CalendarAvailability availability,
    ICalendarDatabase calendarDatabase,
    IBusinessClientRepository businessClients)
    : IRequestHandler<CreateAppointmentCommand, Result<Guid>>
{
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<Guid>> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return Result<Guid>.Fail("El usuario autenticado no tiene un negocio asignado.", ErrorType.Forbidden);
        }

        var business = await _businessRepository.GetByIdAsync(businessId, cancellationToken);
        if (business == null)
        {
            return Result<Guid>.Fail("El negocio especificado no existe.");
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            EmployeeId = request.EmployeeId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ClientName = request.ClientName.Trim(),
            ClientEmail = request.ClientEmail?.Trim() ?? string.Empty,
            ClientPhone = request.ClientPhone?.Trim() ?? string.Empty,
            Status = AppointmentStatus.Confirmed,
            CancellationToken = Guid.NewGuid().ToString("N"),
            AgreedTotal = request.AgreedTotal
        };

        return await calendarDatabase.WithBusinessLockAsync(businessId, async token =>
        {
            if (request.EmployeeId is { } employeeId &&
                !(await calendarDatabase.GetEmployeesAsync(businessId, token)).Any(e => e.Id == employeeId))
            {
                return Result<Guid>.Fail("El empleado no está disponible para este negocio.", ErrorType.Validation);
            }

            // El ID se conserva si EF reintenta la transacción.
            if ((await _appointmentRepository.GetByIdAsync(appointment.Id, token))?.BusinessId == businessId)
            {
                return Result<Guid>.Ok(appointment.Id);
            }

            if (await availability.IsOccupiedAsync(businessId, request.EmployeeId, request.StartTime, request.EndTime, token))
            {
                return Result<Guid>.Fail("El horario ya tiene una cita o un bloqueo.", ErrorType.Conflict);
            }

            if (request.BusinessClientId is { } cardId)
            {
                var card = await businessClients.GetByIdAndBusinessIdAsync(cardId, businessId, token);
                if (card is null || card.Id != cardId || card.BusinessId != businessId || card.ArchivedAt is not null)
                {
                    return Result<Guid>.NotFound("El cliente seleccionado ya no está en la cartera activa.");
                }

                appointment.ClientId = card.ClientId;
                card.HistoryAppointmentIds = [.. card.HistoryAppointmentIds, appointment.Id];
                await businessClients.UpdateAsync(card, token);
            }

            await _appointmentRepository.AddAsync(appointment, token);
            await _unitOfWork.SaveChangesAsync(token);
            return Result<Guid>.Ok(appointment.Id, "Cita agendada exitosamente.");
        }, cancellationToken);
    }
}
