using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Handler para eliminar un bloqueo de agenda.
/// Relacionado con: US-008 (Bloqueo Manual — acción de desbloqueo)
/// </summary>
public class DeleteScheduleBlockHandler(IScheduleBlockRepository scheduleBlockRepository)
    : IRequestHandler<DeleteScheduleBlockCommand, Result>
{
    private readonly IScheduleBlockRepository _scheduleBlockRepository = scheduleBlockRepository;

    public async Task<Result> Handle(
        DeleteScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar bloqueo por ID
        var block = await _scheduleBlockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block == null)
        {
            return Result.Fail("Bloqueo no encontrado.");
        }

        // 2. Verificar pertenencia al negocio si se especifica BusinessId
        if (request.BusinessId != Guid.Empty && block.BusinessId != request.BusinessId)
        {
            return Result.Fail("No tiene autorización para eliminar este bloqueo.");
        }

        // 3. Eliminar
        await _scheduleBlockRepository.DeleteAsync(block, cancellationToken);

        // 4. Retornar éxito
        return Result.Ok("Bloqueo eliminado exitosamente.");
    }
}
