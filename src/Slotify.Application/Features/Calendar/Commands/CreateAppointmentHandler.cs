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
    ICurrentUserService currentUserService)
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
            return Result<Guid>.Fail("El usuario autenticado no tiene un negocio asignado.");
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
            ClientEmail = !string.IsNullOrWhiteSpace(request.ClientEmail) 
                ? request.ClientEmail.Trim() 
                : "cliente@ejemplo.com",
            ClientPhone = !string.IsNullOrWhiteSpace(request.ClientPhone) 
                ? request.ClientPhone.Trim() 
                : "+52 55 1234 5678",
            Status = AppointmentStatus.Confirmed,
            CancellationToken = Guid.NewGuid().ToString("N"),
            AgreedTotal = request.AgreedTotal
        };

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(appointment.Id, "Cita agendada exitosamente.");
    }
}
