using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Calendar.Commands;

/// <summary>
/// Handler para orquestar la creación y persistencia de una nueva cita en el calendario.
/// Cumple con Single Responsibility (SRP) e Inversión de Dependencias (DIP), inyectando solo abstracciones.
/// </summary>
public class CreateAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IBusinessRepository businessRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAppointmentCommand, Result<Guid>>
{
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<Guid>> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var businessId = request.BusinessId;

        // Resolución de negocio por defecto si viene vacío (ej. explorador sin selección explícita)
        if (businessId == Guid.Empty)
        {
            var existingBusinesses = await _businessRepository.GetAllAsync(cancellationToken);
            if (existingBusinesses.Count > 0)
            {
                businessId = existingBusinesses[0].Id;
            }
            else
            {
                var newBusiness = new Business
                {
                    Name = "Mi Negocio",
                    Slug = "mi-negocio-" + Guid.NewGuid().ToString("N")[..6],
                    Phone = "+52 55 1234 5678",
                    SectorTemplateId = 2
                };
                await _businessRepository.AddAsync(newBusiness, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                businessId = newBusiness.Id;
            }
        }
        else
        {
            var business = await _businessRepository.GetByIdAsync(businessId, cancellationToken);
            if (business == null)
            {
                return Result<Guid>.Fail("El negocio especificado no existe.");
            }
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
