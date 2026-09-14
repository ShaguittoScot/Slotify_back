using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Calendar.DTOs;

namespace Slotify.Application.Features.Calendar.Queries;

/// <summary>
/// Query para obtener los slots del calendario en un rango de fechas.
/// Relacionado con: US-003 (Navegación Multiformato en Calendario Táctil)
/// 
/// Combina datos de: citas, bloqueos_agenda, y horarios_disponibilidad
/// para construir la vista unificada del calendario.
/// Filtra por el negocio del admin autenticado.
/// </summary>
public record GetCalendarSlotsQuery : IRequest<Result<CalendarResponse>>
{
    /// <summary>ID del negocio (extraído del JWT del admin autenticado).</summary>
    public required Guid BusinessId { get; init; }

    /// <summary>Fecha de inicio del rango a consultar.</summary>
    public required DateTime StartDate { get; init; }

    /// <summary>Fecha de fin del rango a consultar.</summary>
    public required DateTime EndDate { get; init; }

    /// <summary>Filtro opcional por empleado.</summary>
    public Guid? EmployeeId { get; init; }
}
