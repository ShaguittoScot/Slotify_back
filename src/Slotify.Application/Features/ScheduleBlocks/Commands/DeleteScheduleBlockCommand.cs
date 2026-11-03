using MediatR;
using Slotify.Application.Common.Models;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Comando para eliminar (desbloquear) un bloqueo de agenda existente.
/// Relacionado con: US-008 (Bloqueo Manual — acción de desbloqueo), US-022 (Negocio del token).
/// El BusinessId se obtiene exclusivamente de ICurrentUserService.
/// </summary>
public record DeleteScheduleBlockCommand : IRequest<Result>
{
    /// <summary>ID del bloqueo a eliminar.</summary>
    public required long BlockId { get; init; }
}
