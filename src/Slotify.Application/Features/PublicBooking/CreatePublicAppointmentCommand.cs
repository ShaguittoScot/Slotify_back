using FluentValidation;
using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Calendar;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.PublicBooking;

public record CreatePublicAppointmentCommand : IRequest<Result<PublicAppointmentConfirmationDto>>
{
    public string Slug { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public int? DurationMinutes { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string? ClientPhone { get; init; }
    public string? ClientEmail { get; init; }
    public decimal AgreedTotal { get; init; }
    public Guid? ClientId { get; init; }
    public string? Notes { get; init; }
}

public sealed class CreatePublicAppointmentValidator : AbstractValidator<CreatePublicAppointmentCommand>
{
    public CreatePublicAppointmentValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("El identificador del negocio es obligatorio.");

        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("El nombre del cliente es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.ClientPhone)
            .MaximumLength(Appointment.PhoneMaxLength)
            .WithMessage($"El teléfono no puede exceder {Appointment.PhoneMaxLength} caracteres.");

        RuleFor(x => x.ClientEmail)
            .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres.")
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ClientEmail))
            .WithMessage("El formato del correo electrónico no es válido.");

        RuleFor(x => x.StartTime)
            .Must(d => d.Kind == DateTimeKind.Utc)
            .WithMessage("La fecha de inicio debe enviarse en UTC.");

        RuleFor(x => x.EndTime)
            .Must((cmd, end) => !end.HasValue || (end.Value.Kind == DateTimeKind.Utc && end.Value > cmd.StartTime))
            .WithMessage("La fecha de fin debe ser posterior al inicio y estar en UTC.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(5, 480)
            .When(x => x.DurationMinutes.HasValue)
            .WithMessage("La duración debe estar entre 5 y 480 minutos.");

        RuleFor(x => x.AgreedTotal)
            .GreaterThanOrEqualTo(0).WithMessage("El importe no puede ser negativo.");
    }
}

public sealed class CreatePublicAppointmentHandler(
    IBusinessRepository businesses,
    IAppointmentRepository appointments,
    IClientRepository clients,
    IUnitOfWork unitOfWork,
    CalendarAvailability availability,
    ICalendarDatabase calendarDatabase)
    : IRequestHandler<CreatePublicAppointmentCommand, Result<PublicAppointmentConfirmationDto>>
{
    public async Task<Result<PublicAppointmentConfirmationDto>> Handle(
        CreatePublicAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var business = await businesses.GetBySlugAsync(request.Slug.Trim(), cancellationToken);
        if (business is null)
            return Result<PublicAppointmentConfirmationDto>.NotFound("Negocio no encontrado.");

        var endTime = request.EndTime ?? request.StartTime.AddMinutes(request.DurationMinutes ?? 30);
        if (endTime <= request.StartTime)
            return Result<PublicAppointmentConfirmationDto>.Fail("El rango de horario no es válido.", ErrorType.Validation);

        Guid? resolvedClientId = null;
        if (request.ClientId is { } supaOrClientId && supaOrClientId != Guid.Empty)
        {
            var client = await clients.GetBySupabaseIdAsync(supaOrClientId, cancellationToken)
                ?? await clients.GetByIdAsync(supaOrClientId, cancellationToken);
            resolvedClientId = client?.Id;
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            ClientId = resolvedClientId,
            EmployeeId = null,
            StartTime = request.StartTime,
            EndTime = endTime,
            ClientName = request.ClientName.Trim(),
            ClientEmail = request.ClientEmail?.Trim() ?? string.Empty,
            ClientPhone = request.ClientPhone?.Trim() ?? string.Empty,
            Status = AppointmentStatus.Confirmed,
            CancellationToken = Guid.NewGuid().ToString("N"),
            AgreedTotal = request.AgreedTotal,
        };

        return await calendarDatabase.WithBusinessLockAsync(business.Id, async token =>
        {
            if (await availability.IsOccupiedAsync(business.Id, null, appointment.StartTime, appointment.EndTime, token))
            {
                return Result<PublicAppointmentConfirmationDto>.Fail(
                    "El horario seleccionado ya no está disponible.",
                    ErrorType.Conflict);
            }

            await appointments.AddAsync(appointment, token);
            await unitOfWork.SaveChangesAsync(token);

            return Result<PublicAppointmentConfirmationDto>.Ok(
                new PublicAppointmentConfirmationDto(
                    appointment.Id,
                    business.Name,
                    business.Slug,
                    appointment.StartTime,
                    appointment.EndTime,
                    appointment.ClientName,
                    appointment.AgreedTotal,
                    "confirmed",
                    appointment.CancellationToken),
                "Cita agendada exitosamente.");
        }, cancellationToken);
    }
}
