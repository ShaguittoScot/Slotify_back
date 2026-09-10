using MediatR;
using Slotify.Application.Common.Models;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Handler para crear un bloqueo de agenda.
/// Relacionado con: US-008
/// 
/// DEPENDENCIAS REQUERIDAS:
/// - IScheduleBlockRepository: crear bloqueo
/// - IAppointmentRepository: verificar empalmes con citas (opcional pero recomendado)
/// 
/// TODO: Implementar lógica completa.
/// </summary>
public class CreateScheduleBlockHandler : IRequestHandler<CreateScheduleBlockCommand, Result<long>>
{
    // TODO: Inyectar dependencias via constructor

    public async Task<Result<long>> Handle(
        CreateScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar flujo:
        // 1. (Opcional) Verificar que no hay citas confirmadas en ese rango
        //    → IAppointmentRepository.GetByDateRangeAsync()
        //    → Si hay citas, retornar warning o error según regla de negocio
        //
        // 2. Crear entidad ScheduleBlock desde el command
        //
        // 3. Persistir → IScheduleBlockRepository.CreateAsync()
        //
        // 4. Retornar Result.Ok(block.Id)

        throw new NotImplementedException(
            "US-008: Pendiente implementar CreateScheduleBlockHandler.");
    }
}
