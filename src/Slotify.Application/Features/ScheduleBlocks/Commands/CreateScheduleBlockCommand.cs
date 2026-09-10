using MediatR;
using Slotify.Application.Common.Models;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Comando para crear un bloqueo manual en la agenda.
/// Relacionado con: US-008 (Bloqueo Manual de Horarios No Disponibles)
/// 
/// Flujo:
/// 1. Validar rango de fechas (inicio &lt; fin)
/// 2. Verificar que no se empalme con citas confirmadas
/// 3. Crear el bloqueo en bloqueos_agenda
/// 4. Retornar ID del bloqueo creado
/// </summary>
public record CreateScheduleBlockCommand : IRequest<Result<long>>
{
    /// <summary>ID del negocio (del JWT del admin autenticado).</summary>
    public required Guid BusinessId { get; init; }

    /// <summary>ID del empleado afectado. Null = aplica a todo el negocio.</summary>
    public Guid? EmployeeId { get; init; }

    /// <summary>Inicio del bloqueo (fecha_hora_inicio).</summary>
    public required DateTime StartDateTime { get; init; }

    /// <summary>Fin del bloqueo (fecha_hora_fin).</summary>
    public required DateTime EndDateTime { get; init; }

    /// <summary>Motivo del bloqueo, opcional (motivo).</summary>
    public string? Reason { get; init; }
}
