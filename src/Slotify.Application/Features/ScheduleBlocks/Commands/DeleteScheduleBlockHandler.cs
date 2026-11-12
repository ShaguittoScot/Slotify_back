using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Handler para eliminar un bloqueo de agenda.
/// Cumple con SRP, DIP y Unit of Work delegando la persistencia a IUnitOfWork.
/// Relacionado con: US-008, US-022 y US-023.
/// </summary>
public class DeleteScheduleBlockHandler(
    IScheduleBlockRepository scheduleBlockRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteScheduleBlockCommand, Result>
{
    public const string ForbiddenError = "FORBIDDEN_BUSINESS_MISMATCH";

    private readonly IScheduleBlockRepository _scheduleBlockRepository = scheduleBlockRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
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

        // 3. Eliminar a través del repositorio y persistir vía Unit of Work (SRP)
        await _scheduleBlockRepository.DeleteAsync(block, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Retornar éxito
        return Result.Ok("Bloqueo eliminado exitosamente.");
    }
}
