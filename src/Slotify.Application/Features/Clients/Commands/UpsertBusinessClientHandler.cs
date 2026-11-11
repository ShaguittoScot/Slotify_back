using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Clients.DTOs;
using Slotify.Application.Features.Clients.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Clients.Commands;

/// <summary>
/// Handler para registrar o actualizar la ficha de un cliente dentro de la cartera exclusiva del negocio autenticado.
/// Relacionado con: US-026 (Construcción de módulo de clientes) y US-022 (Aislamiento por BusinessId).
/// Garantías de seguridad e integridad:
/// 1. Nunca consulta ni modifica la tabla global `clientes` (B2C) ni fichas de otros negocios.
/// 2. Un PUT usa una ficha del negocio o una referencia global presente en sus citas. Para cualquier otro ID devuelve 404.
/// 3. El historial se consulta sin modificar las citas.
/// 4. Nunca fusiona clientes homónimos ni clientes que comparten teléfono con distinto nombre.
/// 5. Persiste clientes sin citas previas y notas internas en `clientes_negocio` sin generar correos ficticios.
/// </summary>
public class UpsertBusinessClientHandler(
    IBusinessClientRepository businessClientRepository,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpsertBusinessClientCommand, Result<BusinessClientDto>>
{
    private const string DefaultEmailPlaceholder = "cliente@ejemplo.com";
    private const string DefaultPhonePlaceholder = "+52 55 1234 5678";
    private const string SyntheticEmailDomain = "@clientes.slotify.app";

    private readonly IBusinessClientRepository _businessClientRepository = businessClientRepository;
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<BusinessClientDto>> Handle(
        UpsertBusinessClientCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return Result<BusinessClientDto>.Fail(
                "El usuario autenticado no tiene un negocio asignado.",
                ErrorType.Forbidden);
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var fullName = string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}".Trim();
        var normalizedEmail = SanitizeEmail(request.Email);
        var trimmedPhone = SanitizePhone(request.Phone);
        var normalizedNotes = request.Notes?.Trim();
        var now = DateTime.UtcNow;

        var allBusinessAppointments = await _appointmentRepository.GetByBusinessIdAsync(businessId, cancellationToken);
        var businessAppointments = allBusinessAppointments
            .Where(a => a.BusinessId == businessId)
            .ToList();

        var existingBusinessClients = (await _businessClientRepository.GetByBusinessIdAsync(businessId, cancellationToken))
            .Where(c => c.BusinessId == businessId)
            .ToList();

        // Las fichas previas pueden conservar su teléfono sin bloquear la edición de otros campos.
        if (trimmedPhone.Length > Appointment.PhoneMaxLength &&
            !existingBusinessClients.Any(c => c.Id == request.ClientId && c.Phone?.Trim() == trimmedPhone))
        {
            return Result<BusinessClientDto>.Fail(
                $"El teléfono admite hasta {Appointment.PhoneMaxLength} caracteres.", ErrorType.Validation);
        }

        BusinessClient? businessClient = null;

        // 1. Si viene un ClientId (PUT /api/clients/{id} o actualización por ID explícito):
        //    Verificar pertenencia estricta al negocio autenticado. Si no existe en este negocio, retornar 404.
        if (request.IsExplicitUpdateById || (request.ClientId.HasValue && request.ClientId.Value != Guid.Empty))
        {
            var targetId = request.ClientId ?? Guid.Empty;
            if (targetId == Guid.Empty)
            {
                return Result<BusinessClientDto>.NotFound("El cliente especificado no existe en la cartera de este negocio.");
            }

            businessClient = await _businessClientRepository.GetByIdAndBusinessIdAsync(targetId, businessId, cancellationToken);

            // Verificar aislamiento defensivo por si un mock devolviera un registro de otro negocio
            if (businessClient is not null && businessClient.BusinessId != businessId)
            {
                businessClient = null;
            }

            if (businessClient is null)
            {
                // Solo se permite materializar la ficha si este negocio ya tiene citas vinculadas a ese targetId
                var hasAppointmentsInThisBusiness = businessAppointments.Any(
                    a => a.BusinessId == businessId && a.ClientId.HasValue && a.ClientId.Value == targetId);

                if (!hasAppointmentsInThisBusiness)
                {
                    return Result<BusinessClientDto>.NotFound(
                        "El cliente especificado no existe en la cartera de este negocio.");
                }

                businessClient = new BusinessClient
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    ClientId = targetId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = normalizedEmail,
                    Phone = string.IsNullOrWhiteSpace(trimmedPhone) ? null : trimmedPhone,
                    Notes = normalizedNotes,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _businessClientRepository.AddAsync(businessClient, cancellationToken);
                existingBusinessClients.Add(businessClient);
            }
            else
            {
                if (businessClient.ArchivedAt is not null)
                {
                    return Result<BusinessClientDto>.Fail("Restaura la ficha para editarla.", ErrorType.Conflict);
                }

                if (existingBusinessClients.All(c => c.Id != businessClient.Id))
                {
                    existingBusinessClients.Add(businessClient);
                }

                PreserveHistory(businessClient, existingBusinessClients, businessAppointments);
                businessClient.FirstName = firstName;
                businessClient.LastName = lastName;
                businessClient.Email = normalizedEmail;
                businessClient.Phone = string.IsNullOrWhiteSpace(trimmedPhone) ? null : trimmedPhone;
                if (normalizedNotes is not null)
                {
                    businessClient.Notes = normalizedNotes;
                }
                businessClient.UpdatedAt = now;

                await _businessClientRepository.UpdateAsync(businessClient, cancellationToken);
            }
        }
        else
        {
            // 2. Registro vía POST /api/clients (sin ClientId):
            //    Solo reutiliza una ficha existente del mismo negocio si coincide de forma única e inequívoca
            //    tanto en nombre completo como en medio de contacto, sin conflictos de correo ni teléfono.
            var candidates = existingBusinessClients
                .Where(c => IsUnambiguousSamePerson(c, fullName, normalizedEmail, trimmedPhone))
                .ToList();

            if (candidates.Count == 1)
            {
                businessClient = candidates[0];
                if (businessClient.ArchivedAt is not null)
                {
                    return Result<BusinessClientDto>.Fail("El cliente está archivado. Restaura su ficha desde Archivados.", ErrorType.Conflict);
                }

                PreserveHistory(businessClient, existingBusinessClients, businessAppointments);
                businessClient.FirstName = firstName;
                businessClient.LastName = lastName;
                if (!string.IsNullOrWhiteSpace(normalizedEmail))
                {
                    businessClient.Email = normalizedEmail;
                }
                if (!string.IsNullOrWhiteSpace(trimmedPhone))
                {
                    businessClient.Phone = trimmedPhone;
                }
                if (normalizedNotes is not null)
                {
                    businessClient.Notes = normalizedNotes;
                }
                businessClient.UpdatedAt = now;

                await _businessClientRepository.UpdateAsync(businessClient, cancellationToken);
            }
            else
            {
                var newId = Guid.NewGuid();
                businessClient = new BusinessClient
                {
                    Id = newId,
                    BusinessId = businessId,
                    ClientId = null,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = normalizedEmail,
                    Phone = string.IsNullOrWhiteSpace(trimmedPhone) ? null : trimmedPhone,
                    Notes = normalizedNotes,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _businessClientRepository.AddAsync(businessClient, cancellationToken);
                existingBusinessClients.Add(businessClient);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var dto = GetBusinessClientsHandler.BuildPortfolio(existingBusinessClients, businessAppointments)
            .Single(c => c.IsRegistered && c.ClientId == businessClient.Id);
        return Result<BusinessClientDto>.Ok(dto, "Cliente guardado correctamente.");
    }

    private static void PreserveHistory(
        BusinessClient client, IReadOnlyList<BusinessClient> clients, IReadOnlyList<Appointment> appointments)
    {
        var history = GetBusinessClientsHandler.BuildPortfolio(clients, appointments)
            .Single(c => c.IsRegistered && c.ClientId == client.Id)
            .Appointments;
        client.HistoryAppointmentIds = client.HistoryAppointmentIds.Union(history.Select(a => a.Id)).ToArray();
    }

    private static bool IsUnambiguousSamePerson(
        BusinessClient existing,
        string fullName,
        string normalizedEmail,
        string trimmedPhone)
    {
        if (!string.Equals(NormalizeName(existing.FullName), NormalizeName(fullName), StringComparison.Ordinal))
            return false;

        var existingEmail = SanitizeEmail(existing.Email);
        var existingPhone = NormalizePhoneDigits(existing.Phone);
        var targetPhone = NormalizePhoneDigits(trimmedPhone);

        // Si ambos tienen correo y son distintos → son homónimos distintos
        if (!string.IsNullOrEmpty(existingEmail) && !string.IsNullOrEmpty(normalizedEmail) &&
            !string.Equals(existingEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Si ambos tienen teléfono y son distintos → son homónimos distintos
        if (!string.IsNullOrEmpty(existingPhone) && !string.IsNullOrEmpty(targetPhone) &&
            !string.Equals(existingPhone, targetPhone, StringComparison.Ordinal))
        {
            return false;
        }

        var emailMatches = !string.IsNullOrEmpty(existingEmail) &&
                           string.Equals(existingEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        var phoneMatches = !string.IsNullOrEmpty(existingPhone) &&
                           string.Equals(existingPhone, targetPhone, StringComparison.Ordinal);

        return emailMatches || phoneMatches;
    }

    private static string NormalizeName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        return string.Join(' ', rawName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }

    private static string SanitizeEmail(string? rawEmail)
    {
        if (string.IsNullOrWhiteSpace(rawEmail))
            return string.Empty;

        var trimmed = rawEmail.Trim().ToLowerInvariant();
        if (string.Equals(trimmed, DefaultEmailPlaceholder, StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith(SyntheticEmailDomain, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return trimmed;
    }

    private static string SanitizePhone(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return string.Empty;

        var trimmed = rawPhone.Trim();
        return string.Equals(trimmed, DefaultPhonePlaceholder, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : trimmed;
    }

    private static string NormalizePhoneDigits(string? rawPhone)
    {
        var clean = SanitizePhone(rawPhone);
        if (string.IsNullOrEmpty(clean))
            return string.Empty;

        var digits = new string(clean.Where(char.IsDigit).ToArray());
        return digits.Length >= 7 ? digits : string.Empty;
    }
}
