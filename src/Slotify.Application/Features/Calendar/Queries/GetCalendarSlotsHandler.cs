using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Calendar.DTOs;

namespace Slotify.Application.Features.Calendar.Queries;

/// <summary>
/// Handler para obtener los slots del calendario.
/// Relacionado con: US-003
/// 
/// DEPENDENCIAS REQUERIDAS:
/// - IAppointmentRepository: citas en el rango
/// - IScheduleBlockRepository: bloqueos en el rango
/// - IAvailabilityScheduleRepository: horarios recurrentes
/// 
/// TODO: Implementar la lógica de composición del calendario.
/// </summary>
public class GetCalendarSlotsHandler
    : IRequestHandler<GetCalendarSlotsQuery, Result<CalendarResponse>>
{
    // TODO: Inyectar dependencias via constructor

    public async Task<Result<CalendarResponse>> Handle(
        GetCalendarSlotsQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar flujo completo:
        // 1. Obtener citas del negocio en el rango
        //    → IAppointmentRepository.GetByDateRangeAsync()
        //    → Mapear a CalendarSlotDto con Type = "appointment"
        //
        // 2. Obtener bloqueos del negocio en el rango
        //    → IScheduleBlockRepository.GetByDateRangeAsync()
        //    → Mapear a CalendarSlotDto con Type = "block"
        //
        // 3. Obtener horarios de disponibilidad recurrentes
        //    → IAvailabilityScheduleRepository.GetByBusinessIdAsync()
        //    → Expandir los horarios recurrentes en slots concretos para el rango
        //    → Mapear a CalendarSlotDto con Type = "available"
        //
        // 4. Combinar y ordenar todos los slots por StartTime
        // 5. Retornar CalendarResponse con el rango y los slots

        throw new NotImplementedException(
            "US-003: Pendiente implementar GetCalendarSlotsHandler. " +
            "Requiere componer datos de 3 tablas: citas, bloqueos, y horarios.");
    }
}
