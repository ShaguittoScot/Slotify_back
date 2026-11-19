using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Clients.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Clients.Queries;

/// <summary>
/// Handler para construir la cartera de clientes del negocio autenticado combinando las fichas
/// registradas en el negocio (`clientes_negocio`) y el historial de citas (`citas`), con aislamiento
/// estricto por BusinessId.
/// Relacionado con: US-026 (Construcción de módulo de clientes).
/// </summary>
public class GetBusinessClientsHandler(
    IBusinessClientRepository businessClientRepository,
    IAppointmentRepository appointmentRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetBusinessClientsQuery, Result<IReadOnlyList<BusinessClientDto>>>
{
    private const string DefaultEmailPlaceholder = "cliente@ejemplo.com";
    private const string DefaultPhonePlaceholder = "+52 55 1234 5678";
    private const string SyntheticEmailDomain = "@clientes.slotify.app";

    private readonly IBusinessClientRepository _businessClientRepository = businessClientRepository;
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<IReadOnlyList<BusinessClientDto>>> Handle(
        GetBusinessClientsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return Result<IReadOnlyList<BusinessClientDto>>.Fail(
                "El usuario autenticado no tiene un negocio asignado.",
                ErrorType.Forbidden);
        }

        var registeredClients = (await _businessClientRepository.GetByBusinessIdAsync(businessId, cancellationToken))
            .Where(c => c.BusinessId == businessId)
            .ToList();

        var appointments = (await _appointmentRepository.GetByBusinessIdAsync(businessId, cancellationToken))
            .Where(a => a.BusinessId == businessId)
            .ToList();

        IEnumerable<BusinessClientDto> clients = BuildPortfolio(registeredClients, appointments);
        if (!request.IncludeArchived)
        {
            clients = clients.Where(c => c.ArchivedAt is null);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            clients = clients.Where(c =>
                c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Phone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
        }

        return Result<IReadOnlyList<BusinessClientDto>>.Ok(clients
            .OrderByDescending(c => c.NextVisitAt.HasValue)
            .ThenByDescending(c => c.LastVisitAt ?? c.CreatedAt)
            .ThenBy(c => c.FullName)
            .ToList());
    }

    internal static IEnumerable<BusinessClientDto> BuildPortfolio(
        IReadOnlyList<BusinessClient> registeredClients,
        IReadOnlyList<Appointment> appointments)
    {
        // Mapa de cubetas: cada BusinessClient registrado tiene su propia cubeta (incluso con 0 citas)
        var buckets = new Dictionary<string, ClientBucket>(StringComparer.OrdinalIgnoreCase);
        var globalClientIdToBucketKey = new Dictionary<Guid, string>();
        var historicalAppointmentToBucketKey = new Dictionary<Guid, string>();

        foreach (var reg in registeredClients)
        {
            var bucketKey = $"bizclient:{reg.Id:D}";
            buckets[bucketKey] = new ClientBucket(bucketKey, reg);
            if (reg.ClientId.HasValue && reg.ClientId.Value != Guid.Empty)
            {
                globalClientIdToBucketKey.TryAdd(reg.ClientId.Value, bucketKey);
            }
            foreach (var appointmentId in reg.HistoryAppointmentIds)
            {
                historicalAppointmentToBucketKey.TryAdd(appointmentId, bucketKey);
            }
        }

        // Asociar citas del negocio sin fusionar homónimos ni teléfonos compartidos
        foreach (var appointment in appointments)
        {
            string targetBucketKey;

            if (historicalAppointmentToBucketKey.TryGetValue(appointment.Id, out var historicalBucketKey) &&
                (appointment.ClientId is null || appointment.ClientId == Guid.Empty ||
                 appointment.ClientId == buckets[historicalBucketKey].RegisteredClient?.ClientId))
            {
                targetBucketKey = historicalBucketKey;
            }
            else if (appointment.ClientId is { } apptClientId && apptClientId != Guid.Empty)
            {
                if (globalClientIdToBucketKey.TryGetValue(apptClientId, out var mappedKey))
                {
                    targetBucketKey = mappedKey;
                }
                else
                {
                    targetBucketKey = $"id:{apptClientId:D}";
                }
            }
            else
            {
                // Cita sin ClientId: verificar si coincide de forma única e inequívoca con una ficha registrada
                var matchingRegistered = registeredClients
                    .Where(reg => DoesUnlinkedAppointmentMatchRegistered(appointment, reg))
                    .ToList();

                if (matchingRegistered.Count == 1)
                {
                    targetBucketKey = $"bizclient:{matchingRegistered[0].Id:D}";
                }
                else
                {
                    targetBucketKey = ResolveUnlinkedAppointmentGroupingKey(appointment);
                }
            }

            if (!buckets.TryGetValue(targetBucketKey, out var bucket))
            {
                bucket = new ClientBucket(targetBucketKey, null);
                buckets[targetBucketKey] = bucket;
            }

            bucket.Appointments.Add(appointment);
        }

        var now = DateTime.UtcNow;
        return buckets.Values.Select(b => MapBucketToDto(b, now));
    }

    private sealed class ClientBucket(string key, BusinessClient? registeredClient)
    {
        public string Key { get; } = key;
        public BusinessClient? RegisteredClient { get; } = registeredClient;
        public List<Appointment> Appointments { get; } = [];
    }

    /// <summary>
    /// Agrupa citas sin ClientId exigiendo coincidencia tanto de nombre como de contacto.
    /// Evita fusionar homónimos (mismo nombre, distinto teléfono/correo) y familiares con teléfono compartido (distinto nombre).
    /// </summary>
    private static string ResolveUnlinkedAppointmentGroupingKey(Appointment appointment)
    {
        var rawName = !string.IsNullOrWhiteSpace(appointment.ClientName)
            ? appointment.ClientName
            : (appointment.Client?.FullName ?? "Cliente");

        var normName = NormalizeName(rawName);
        var email = SanitizeEmail(!string.IsNullOrWhiteSpace(appointment.ClientEmail) ? appointment.ClientEmail : appointment.Client?.Email);
        var phoneDigits = NormalizePhoneDigits(!string.IsNullOrWhiteSpace(appointment.ClientPhone) ? appointment.ClientPhone : appointment.Client?.Phone);

        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phoneDigits))
        {
            // Sin ningún dato de contacto ni ClientId: mantener aislada por cita para no fusionar homónimos anónimos
            return $"unlinked-appt:{appointment.Id:D}";
        }

        return $"unlinked:name:{normName}|email:{email}|phone:{phoneDigits}";
    }

    private static bool DoesUnlinkedAppointmentMatchRegistered(Appointment appointment, BusinessClient registered)
    {
        var apptName = NormalizeName(appointment.ClientName);
        var regName = NormalizeName(registered.FullName);

        if (string.IsNullOrEmpty(apptName) || !string.Equals(apptName, regName, StringComparison.Ordinal))
            return false;

        var apptEmail = SanitizeEmail(appointment.ClientEmail);
        var regEmail = SanitizeEmail(registered.Email);

        var apptPhone = NormalizePhoneDigits(appointment.ClientPhone);
        var regPhone = NormalizePhoneDigits(registered.Phone);

        if (!string.IsNullOrEmpty(apptEmail) && !string.IsNullOrEmpty(regEmail) &&
            !string.Equals(apptEmail, regEmail, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(apptPhone) && !string.IsNullOrEmpty(regPhone) &&
            !string.Equals(apptPhone, regPhone, StringComparison.Ordinal))
        {
            return false;
        }

        var emailMatches = !string.IsNullOrEmpty(apptEmail) &&
                           string.Equals(apptEmail, regEmail, StringComparison.OrdinalIgnoreCase);
        var phoneMatches = !string.IsNullOrEmpty(apptPhone) &&
                           string.Equals(apptPhone, regPhone, StringComparison.Ordinal);

        return emailMatches || phoneMatches;
    }

    private static BusinessClientDto MapBucketToDto(ClientBucket bucket, DateTime nowUtc)
    {
        var orderedAppointments = bucket.Appointments
            .OrderByDescending(a => a.StartTime)
            .ToList();

        var reg = bucket.RegisteredClient;
        var latestAppt = orderedAppointments.FirstOrDefault();
        var linkedGlobalClient = orderedAppointments
            .Select(a => a.Client)
            .FirstOrDefault(c => c is not null);

        var editableId = reg?.Id
            ?? orderedAppointments.Select(a => a.ClientId).FirstOrDefault(id => id.HasValue && id.Value != Guid.Empty)
            ?? linkedGlobalClient?.Id;

        string fullName;
        if (!string.IsNullOrWhiteSpace(reg?.FullName))
        {
            fullName = reg.FullName.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(latestAppt?.ClientName))
        {
            fullName = latestAppt.ClientName.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(linkedGlobalClient?.FullName))
        {
            fullName = linkedGlobalClient.FullName.Trim();
        }
        else
        {
            fullName = "Cliente";
        }

        var (firstName, lastName) = SplitFullName(
            fullName,
            reg?.FirstName ?? linkedGlobalClient?.FirstName,
            reg?.LastName ?? linkedGlobalClient?.LastName);

        var email = SanitizeEmail(reg?.Email);
        if (string.IsNullOrEmpty(email))
        {
            email = orderedAppointments
                .Select(a => !string.IsNullOrWhiteSpace(SanitizeEmail(a.ClientEmail))
                    ? SanitizeEmail(a.ClientEmail)
                    : SanitizeEmail(a.Client?.Email))
                .FirstOrDefault(e => !string.IsNullOrEmpty(e)) ?? string.Empty;
        }

        var phone = SanitizePhone(reg?.Phone);
        if (string.IsNullOrEmpty(phone))
        {
            phone = orderedAppointments
                .Select(a => !string.IsNullOrWhiteSpace(SanitizePhone(a.ClientPhone))
                    ? SanitizePhone(a.ClientPhone)
                    : SanitizePhone(a.Client?.Phone))
                .FirstOrDefault(p => !string.IsNullOrEmpty(p)) ?? string.Empty;
        }

        var validAppointments = orderedAppointments
            .Where(a => a.Status is AppointmentStatus.Confirmed or AppointmentStatus.Completed)
            .ToList();

        var completedAppointments = orderedAppointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .ToList();

        var cancelledCount = orderedAppointments.Count(a => a.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow);

        // TotalSpent suma el importe pactado de las citas completadas.
        var totalSpent = completedAppointments.Sum(a => a.AgreedTotal);

        var pastValidVisits = validAppointments
            .Where(a => a.StartTime <= nowUtc)
            .OrderByDescending(a => a.StartTime)
            .ToList();

        var upcomingVisits = validAppointments
            .Where(a => a.StartTime > nowUtc && a.Status == AppointmentStatus.Confirmed)
            .OrderBy(a => a.StartTime)
            .ToList();

        DateTime createdAt;
        if (reg is not null && reg.CreatedAt != default)
        {
            createdAt = reg.CreatedAt;
        }
        else if (orderedAppointments.Count > 0)
        {
            createdAt = orderedAppointments.Min(a => a.CreatedAt != default ? a.CreatedAt : a.StartTime);
        }
        else
        {
            createdAt = nowUtc;
        }

        return new BusinessClientDto
        {
            Id = editableId?.ToString("D") ?? bucket.Key,
            ClientId = editableId,
            IsRegistered = reg is not null,
            ArchivedAt = reg?.ArchivedAt,
            FullName = fullName,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Notes = reg?.Notes,
            TotalVisits = validAppointments.Count,
            CompletedVisits = completedAppointments.Count,
            CancelledVisits = cancelledCount,
            TotalSpent = totalSpent,
            LastVisitAt = pastValidVisits.Count > 0 ? pastValidVisits[0].StartTime : null,
            NextVisitAt = upcomingVisits.Count > 0 ? upcomingVisits[0].StartTime : null,
            CreatedAt = createdAt,
            Appointments = orderedAppointments
                .Select(a => new ClientAppointmentItemDto
                {
                    Title = a.ServiceTitle,
                    Id = a.Id,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = MapStatusToCode(a.Status),
                    AgreedTotal = a.AgreedTotal,
                    EmployeeId = a.EmployeeId
                })
                .ToList()
        };
    }

    private static (string FirstName, string LastName) SplitFullName(
        string fullName,
        string? explicitFirstName,
        string? explicitLastName)
    {
        if (!string.IsNullOrWhiteSpace(explicitFirstName))
        {
            return (explicitFirstName.Trim(), explicitLastName?.Trim() ?? string.Empty);
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return ("Cliente", string.Empty);
        if (parts.Length == 1)
            return (parts[0], string.Empty);

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string NormalizeName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "cliente";

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

    private static string MapStatusToCode(AppointmentStatus status) =>
        status switch
        {
            AppointmentStatus.Confirmed => "confirmed",
            AppointmentStatus.Completed => "completed",
            AppointmentStatus.Cancelled => "cancelled",
            AppointmentStatus.NoShow => "no_show",
            _ => "confirmed"
        };
}
