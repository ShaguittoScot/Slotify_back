namespace Slotify.Application.Features.Calendar.DTOs;

/// <summary>
/// DTO para un slot del calendario (puede ser cita, bloqueo, o disponibilidad).
/// Relacionado con: US-003 (Navegación Multiformato en Calendario Táctil)
/// </summary>
public record CalendarSlotDto
{
    /// <summary>Tipo de slot: "appointment", "block", "available"</summary>
    public required string Type { get; init; }

    /// <summary>ID del recurso original (cita, bloqueo, etc.)</summary>
    public required string ResourceId { get; init; }

    /// <summary>Inicio del slot (UTC ISO 8601).</summary>
    public required DateTime StartTime { get; init; }

    /// <summary>Fin del slot (UTC ISO 8601).</summary>
    public required DateTime EndTime { get; init; }

    /// <summary>Título para mostrar en el calendario.</summary>
    public required string Title { get; init; }

    /// <summary>Estado visual: "confirmed", "cancelled", "blocked", "available"</summary>
    public required string Status { get; init; }

    /// <summary>Nombre del cliente (solo para citas).</summary>
    public string? ClientName { get; init; }

    /// <summary>Motivo del bloqueo (solo para bloqueos).</summary>
    public string? BlockReason { get; init; }
}

/// <summary>
/// Respuesta completa del calendario para un rango de fechas.
/// </summary>
public record CalendarResponse
{
    /// <summary>Fecha de inicio del rango consultado.</summary>
    public required DateTime RangeStart { get; init; }

    /// <summary>Fecha de fin del rango consultado.</summary>
    public required DateTime RangeEnd { get; init; }

    /// <summary>Lista de todos los slots (citas + bloqueos + disponibilidad).</summary>
    public required List<CalendarSlotDto> Slots { get; init; }
}
