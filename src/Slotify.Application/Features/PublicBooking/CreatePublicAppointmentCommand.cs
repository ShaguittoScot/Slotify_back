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
    public bool IsVerifiedAccount { get; init; }
    public string? VerifiedEmail { get; init; }
    public string? VerifiedFirstName { get; init; }
    public string? VerifiedLastName { get; init; }
    public string? VerifiedFullName { get; init; }
    public string? VerifiedPhone { get; init; }
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
    IBusinessClientRepository businessClients,
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

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            ClientId = null,
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

            var resolvedClient = await ResolveRegisteredAccountAsync(request, token);
            if (resolvedClient is not null)
            {
                appointment.ClientId = resolvedClient.Id;
                if (string.IsNullOrWhiteSpace(appointment.ClientEmail) && !string.IsNullOrWhiteSpace(resolvedClient.Email))
                {
                    appointment.ClientEmail = LimitLength(resolvedClient.Email, 150);
                }

                await IntegrateIntoOfficialDirectoryAsync(business.Id, resolvedClient, appointment, token);
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
                    appointment.CancellationToken,
                    resolvedClient is not null),
                "Cita agendada exitosamente.");
        }, cancellationToken);
    }

    private async Task<Client?> ResolveRegisteredAccountAsync(
        CreatePublicAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ClientId is not { } supaOrClientId || supaOrClientId == Guid.Empty)
        {
            return null;
        }

        var existing = await clients.GetBySupabaseIdAsync(supaOrClientId, cancellationToken)
            ?? await clients.GetByIdAsync(supaOrClientId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var accountEmail = (request.VerifiedEmail ?? request.ClientEmail)?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(accountEmail))
        {
            var byEmail = await clients.GetByEmailAsync(accountEmail, cancellationToken);
            if (byEmail is not null && (byEmail.SupabaseId is null || byEmail.SupabaseId == supaOrClientId))
            {
                if (byEmail.SupabaseId is null && request.IsVerifiedAccount)
                {
                    byEmail.SupabaseId = supaOrClientId;
                    await clients.UpdateAsync(byEmail, cancellationToken);
                }
                return byEmail;
            }
        }

        if (!request.IsVerifiedAccount || string.IsNullOrWhiteSpace(accountEmail))
        {
            return null;
        }

        var (firstName, lastName) = ResolveNameParts(
            request.VerifiedFirstName,
            request.VerifiedLastName,
            request.VerifiedFullName,
            request.ClientName);
        var phone = !string.IsNullOrWhiteSpace(request.VerifiedPhone)
            ? request.VerifiedPhone.Trim()
            : request.ClientPhone?.Trim();

        var createdClient = new Client
        {
            Id = supaOrClientId,
            SupabaseId = supaOrClientId,
            FirstName = LimitLength(firstName, 100),
            LastName = LimitLength(lastName, 100),
            Email = LimitLength(accountEmail, 150),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : LimitLength(phone, 30),
        };

        await clients.AddAsync(createdClient, cancellationToken);
        return createdClient;
    }

    private async Task IntegrateIntoOfficialDirectoryAsync(
        Guid businessId,
        Client registeredClient,
        Appointment appointment,
        CancellationToken cancellationToken)
    {
        var card = await businessClients.GetByClientIdAndBusinessIdAsync(registeredClient.Id, businessId, cancellationToken);
        var normalizedEmail = registeredClient.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (card is null && !string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var existingCards = await businessClients.GetByBusinessIdAsync(businessId, cancellationToken) ?? [];
            var unlinkedEmailMatches = existingCards
                .Where(c => c.BusinessId == businessId
                         && c.ClientId is null
                         && c.ArchivedAt is null
                         && string.Equals(c.Email.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (unlinkedEmailMatches.Count == 1)
            {
                card = unlinkedEmailMatches[0];
                card.ClientId = registeredClient.Id;
            }
        }

        var resolvedPhone = !string.IsNullOrWhiteSpace(appointment.ClientPhone)
            ? appointment.ClientPhone.Trim()
            : registeredClient.Phone?.Trim();

        if (card is null)
        {
            var (firstName, lastName) = ResolveNameParts(
                registeredClient.FirstName,
                registeredClient.LastName,
                registeredClient.FullName,
                appointment.ClientName);

            card = new BusinessClient
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                ClientId = registeredClient.Id,
                FirstName = LimitLength(firstName, 100),
                LastName = LimitLength(lastName, 100),
                Email = LimitLength(normalizedEmail, 150),
                Phone = string.IsNullOrWhiteSpace(resolvedPhone) ? null : LimitLength(resolvedPhone, 30),
                HistoryAppointmentIds = [appointment.Id],
            };
            await businessClients.AddAsync(card, cancellationToken);
            return;
        }

        if (!card.HistoryAppointmentIds.Contains(appointment.Id))
        {
            card.HistoryAppointmentIds = [.. card.HistoryAppointmentIds, appointment.Id];
        }

        if (string.IsNullOrWhiteSpace(card.Email) && !string.IsNullOrWhiteSpace(normalizedEmail))
        {
            card.Email = LimitLength(normalizedEmail, 150);
        }

        if (string.IsNullOrWhiteSpace(card.Phone) && !string.IsNullOrWhiteSpace(resolvedPhone))
        {
            card.Phone = LimitLength(resolvedPhone, 30);
        }

        if (card.ArchivedAt is not null)
        {
            card.ArchivedAt = null;
        }

        await businessClients.UpdateAsync(card, cancellationToken);
    }

    private static (string FirstName, string LastName) ResolveNameParts(
        string? explicitFirstName,
        string? explicitLastName,
        string? explicitFullName,
        string? fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(explicitFirstName))
        {
            return (explicitFirstName.Trim(), explicitLastName?.Trim() ?? string.Empty);
        }

        var rawFull = !string.IsNullOrWhiteSpace(explicitFullName) ? explicitFullName : fallbackName;
        var parts = (rawFull ?? string.Empty)
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return ("Cliente", string.Empty);
        if (parts.Length == 1)
            return (parts[0], string.Empty);

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string LimitLength(string value, int maxLength) =>
        value.Length <= maxLength ? value : string.Concat(value.EnumerateRunes().Take(maxLength));
}
