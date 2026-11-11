using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Calendar.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using Slotify.Domain.Enums;

namespace Slotify.Application.Features.Calendar.Queries;

/// <summary>
/// Handler para obtener los slots del calendario.
/// Relacionado con: US-003 (Navegación Multiformato en Calendario Táctil), US-022 (Negocio del token).
/// </summary>
public class GetCalendarSlotsHandler(
    IAppointmentRepository appointmentRepository,
    IScheduleBlockRepository scheduleBlockRepository,
    ICurrentUserService currentUserService,
    ICalendarDatabase calendarDatabase,
    IBusinessRepository businessRepository)
    : IRequestHandler<GetCalendarSlotsQuery, Result<CalendarResponse>>
{
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IScheduleBlockRepository _scheduleBlockRepository = scheduleBlockRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<CalendarResponse>> Handle(
        GetCalendarSlotsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return Result<CalendarResponse>.Fail("El usuario autenticado no tiene un negocio asignado.", ErrorType.Forbidden);
        }

        // 1. Obtener citas del negocio en el rango solicitado
        IReadOnlyList<Appointment> appointments;
        if (request.EmployeeId.HasValue)
        {
            appointments = await _appointmentRepository.GetByEmployeeAndDateRangeAsync(
                businessId,
                request.EmployeeId.Value,
                request.StartDate,
                request.EndDate,
                cancellationToken);
        }
        else
        {
            appointments = await _appointmentRepository.GetByDateRangeAsync(
                businessId,
                request.StartDate,
                request.EndDate,
                cancellationToken);
        }

        // 2. Obtener bloqueos de agenda en el rango solicitado
        var blocks = await _scheduleBlockRepository.GetByDateRangeAsync(
            businessId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        if (request.EmployeeId.HasValue)
        {
            blocks = blocks
                .Where(b => !b.EmployeeId.HasValue || b.EmployeeId == request.EmployeeId.Value)
                .ToList();
        }

        var employees = appointments.Any(a => a.EmployeeId.HasValue) || blocks.Any(b => b.EmployeeId.HasValue)
            ? (await calendarDatabase.GetEmployeesAsync(businessId, cancellationToken)).ToDictionary(e => e.Id, e => e.Name)
            : new Dictionary<Guid, string>();

        // 3. Mapear citas a CalendarSlotDto
        var slots = new List<CalendarSlotDto>();

        foreach (var apt in appointments)
        {
            var statusStr = apt.Status == AppointmentStatus.NoShow ? "no_show" : apt.Status.ToString().ToLowerInvariant();
            slots.Add(new CalendarSlotDto
            {
                Type = "appointment",
                ResourceId = apt.Id.ToString(),
                StartTime = apt.StartTime,
                EndTime = apt.EndTime,
                Title = !string.IsNullOrWhiteSpace(apt.ClientName) ? $"Cita - {apt.ClientName}" : "Cita Agendada",
                Status = statusStr,
                ClientName = apt.ClientName,
                ClientPhone = apt.ClientPhone,
                ServicePrice = apt.AgreedTotal > 0 ? $"${apt.AgreedTotal:N2} MXN" : null,
                AgreedTotal = apt.AgreedTotal,
                EmployeeId = apt.EmployeeId,
                BlockReason = null,
                EmployeeName = apt.EmployeeId is { } employeeId ? employees.GetValueOrDefault(employeeId) : null
            });
        }

        // 4. Mapear bloqueos a CalendarSlotDto
        foreach (var blk in blocks)
        {
            slots.Add(new CalendarSlotDto
            {
                Type = "block",
                ResourceId = blk.Id.ToString(),
                StartTime = blk.StartDateTime,
                EndTime = blk.EndDateTime,
                Title = !string.IsNullOrWhiteSpace(blk.Reason) ? blk.Reason : "Horario Bloqueado",
                Status = "blocked",
                ClientName = null,
                ClientPhone = null,
                ServicePrice = null,
                BlockReason = blk.Reason,
                EmployeeId = blk.EmployeeId,
                EmployeeName = blk.EmployeeId is { } employeeId ? employees.GetValueOrDefault(employeeId) : null
            });
        }

        // 5. Ordenar cronológicamente por StartTime
        var orderedSlots = slots.OrderBy(s => s.StartTime).ToList();

        var response = new CalendarResponse
        {
            TimeZone = (await businessRepository.GetByIdAsync(businessId, cancellationToken))?.TimeZone
                ?? "America/Mexico_City",
            RangeStart = request.StartDate,
            RangeEnd = request.EndDate,
            Slots = orderedSlots
        };

        return Result<CalendarResponse>.Ok(response);
    }
}
