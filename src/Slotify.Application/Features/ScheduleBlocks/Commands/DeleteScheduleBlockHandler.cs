using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Handler para eliminar un bloqueo de agenda.
/// Relacionado con: US-008 (Bloqueo Manual — acción de desbloqueo), US-022 (Negocio del token).
/// </summary>
public class DeleteScheduleBlockHandler(
    IScheduleBlockRepository scheduleBlockRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteScheduleBlockCommand, Result>
{
    public const string ForbiddenError = "FORBIDDEN_BUSINESS_MISMATCH";

    private readonly IScheduleBlockRepository _scheduleBlockRepository = scheduleBlockRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result> Handle(
        DeleteScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return Result.Fail(ForbiddenError);
        }

        // 1. Buscar bloqueo por ID
        var block = await _scheduleBlockRepository.GetByIdAsync(request.BlockId, cancellationToken);
        if (block == null)
        {
            return Result.NotFound("Bloqueo no encontrado.");
        }

        // 2. Verificar pertenencia estricta al negocio del token
        if (block.BusinessId != businessId)
        {
            return Result.Fail("No tiene autorización para eliminar este bloqueo.", ErrorType.Forbidden);
        }

        // 3. Eliminar
        await _scheduleBlockRepository.DeleteAsync(block, cancellationToken);

        // 4. Retornar éxito
        return Result.Ok("Bloqueo eliminado exitosamente.");
    }
}
