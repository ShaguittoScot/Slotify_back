using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Notifications.DTOs;
using Slotify.Application.Features.Notifications.Queries;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Slotify.Application.Features.Notifications.Handlers;

/// <summary>
/// Handler para GetUpcomingNotificationsQuery.
/// US-Notifications: Devuelve citas próximas + hash de snapshot para el motor de notificaciones push.
/// 
/// El cliente (frontend) llama a este endpoint:
///   1. Al arrancar la app → almacena snapshot localmente.
///   2. Periódicamente (ej. cada minuto) o al volver al foreground → compara hash.
///   3. Si el hash cambió → dispara notificación inmediata de "cambio en agenda".
///   4. Usa las fechas de las citas para programar recordatorios locales con expo-notifications.
/// </summary>
public class GetUpcomingNotificationsHandler(
    IAppointmentRepository appointmentRepository,
    IBusinessRepository businessRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetUpcomingNotificationsQuery, Result<UpcomingNotificationsResponse>>
{
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<UpcomingNotificationsResponse>> Handle(
        GetUpcomingNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is not { } userId)
            return Result<UpcomingNotificationsResponse>.Fail("Usuario no autenticado.", ErrorType.Forbidden);

        var horizonDays = Math.Clamp(request.HorizonDays, 1, 90);
        var now = DateTime.UtcNow;
        var endDate = now.AddDays(horizonDays);

        var role = _currentUserService.Role?.ToUpperInvariant();
        var businessId = _currentUserService.BusinessId;

        List<UpcomingAppointmentDto> appointments;

        if (role == "CLIENTE" || businessId == null || businessId == Guid.Empty)
        {
            // Vista cliente: citas donde el ClientId coincide con el userId
            appointments = await GetClientAppointmentsAsync(userId, now, endDate, cancellationToken);
        }
        else
        {
            // Vista negocio/empleado: todas las citas del negocio en el horizonte
            appointments = await GetBusinessAppointmentsAsync(businessId.Value, now, endDate, cancellationToken);
        }

        var snapshotHash = ComputeSnapshotHash(appointments);

        return Result<UpcomingNotificationsResponse>.Ok(new UpcomingNotificationsResponse
        {
            Appointments = appointments,
            SnapshotHash = snapshotHash,
            GeneratedAt = now,
            HorizonDays = horizonDays
        });
    }

    private async Task<List<UpcomingAppointmentDto>> GetBusinessAppointmentsAsync(
        Guid businessId, DateTime from, DateTime to, CancellationToken ct)
    {
        var appointments = await _appointmentRepository.GetByDateRangeAsync(businessId, from, to, ct);
        var business = await _businessRepository.GetByIdAsync(businessId, ct);
        var businessName = business?.Name;

        // Cargar nombres de empleados si hay citas con empleado asignado
        var employeeIds = appointments
            .Where(a => a.EmployeeId.HasValue)
            .Select(a => a.EmployeeId!.Value)
            .Distinct()
            .ToHashSet();

        return appointments
            .Where(a => a.Status != AppointmentStatus.Cancelled || IsRecentlyCancelled(a.UpdatedAt ?? a.CreatedAt))
            .Select(a => new UpcomingAppointmentDto
            {
                Id = a.Id.ToString(),
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                ClientName = a.ClientName,
                Status = MapStatus(a.Status),
                EmployeeName = null, // Se resuelve sin consulta adicional pesada
                BusinessName = businessName,
                AgreedTotal = a.AgreedTotal > 0 ? a.AgreedTotal : null
            })
            .OrderBy(a => a.StartTime)
            .ToList();
    }

    private async Task<List<UpcomingAppointmentDto>> GetClientAppointmentsAsync(
        Guid userId, DateTime from, DateTime to, CancellationToken ct)
    {
        // Para clientes: buscar sus citas por ClientId en el horizonte dado
        var allByClient = await _appointmentRepository.GetByClientIdAsync(userId, from, to, ct);

        return allByClient
            .Where(a => a.Status != AppointmentStatus.Cancelled || IsRecentlyCancelled(a.UpdatedAt ?? a.CreatedAt))
            .Select(a => new UpcomingAppointmentDto
            {
                Id = a.Id.ToString(),
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                ClientName = a.ClientName,
                Status = MapStatus(a.Status),
                EmployeeName = null,
                BusinessName = a.Business?.Name,
                AgreedTotal = a.AgreedTotal > 0 ? a.AgreedTotal : null
            })
            .OrderBy(a => a.StartTime)
            .ToList();
    }

    /// <summary>
    /// Una cancelación es "reciente" si ocurrió en las últimas 24h.
    /// Esto permite notificar al usuario de cancelaciones recientes.
    /// </summary>
    private static bool IsRecentlyCancelled(DateTime? changedAt)
        => changedAt.HasValue && (DateTime.UtcNow - changedAt.Value).TotalHours < 24;

    private static string MapStatus(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Confirmed => "confirmed",
        AppointmentStatus.Cancelled => "cancelled",
        AppointmentStatus.Completed => "completed",
        AppointmentStatus.NoShow => "no_show",
        _ => status.ToString().ToLowerInvariant()
    };

    /// <summary>
    /// Calcula un hash SHA-256 del snapshot: ID+Status+StartTime de cada cita.
    /// Si algún campo cambia (reprogramación, cancelación, nueva cita), el hash cambia.
    /// </summary>
    private static string ComputeSnapshotHash(IEnumerable<UpcomingAppointmentDto> appointments)
    {
        var sb = new StringBuilder();
        foreach (var apt in appointments.OrderBy(a => a.Id))
            sb.Append($"{apt.Id}:{apt.Status}:{apt.StartTime:O};");

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
