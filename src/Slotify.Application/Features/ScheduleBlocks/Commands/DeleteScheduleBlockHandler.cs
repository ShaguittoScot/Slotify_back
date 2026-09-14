using MediatR;
using Slotify.Application.Common.Models;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Handler para eliminar un bloqueo de agenda.
/// Relacionado con: US-008
/// 
/// DEPENDENCIAS REQUERIDAS:
/// - IScheduleBlockRepository: buscar y eliminar bloqueo
/// 
/// TODO: Implementar lógica completa.
/// </summary>
public class DeleteScheduleBlockHandler : IRequestHandler<DeleteScheduleBlockCommand, Result>
{
    // TODO: Inyectar dependencias via constructor

    public async Task<Result> Handle(
        DeleteScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar flujo:
        // 1. Buscar bloqueo por ID → IScheduleBlockRepository.GetByIdAsync()
        // 2. Si no existe → Result.Fail("Bloqueo no encontrado")
        // 3. Verificar que el bloqueo pertenece al negocio (BusinessId)
        // 4. Eliminar → IScheduleBlockRepository.DeleteAsync()
        // 5. Retornar Result.Ok("Bloqueo eliminado")

        throw new NotImplementedException(
            "US-008: Pendiente implementar DeleteScheduleBlockHandler.");
    }
}
