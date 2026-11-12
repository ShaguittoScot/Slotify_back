using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.ScheduleBlocks.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Queries;

/// <summary>
/// Handler para consultar los bloqueos de horario de la agenda.
/// Relacionado con: US-008, US-022 y US-023.
/// </summary>
public class GetScheduleBlocksHandler(
    IScheduleBlockRepository scheduleBlockRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetScheduleBlocksQuery, Result<IReadOnlyList<ScheduleBlockDto>>>
{
    private readonly IScheduleBlockRepository _scheduleBlockRepository = scheduleBlockRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<IReadOnlyList<ScheduleBlockDto>>> Handle(
        GetScheduleBlocksQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return Result<IReadOnlyList<ScheduleBlockDto>>.Fail("El usuario autenticado no tiene un negocio asignado.");
        }

        IReadOnlyList<ScheduleBlock> blocks;
        if (request.EmployeeId.HasValue)
        {
            blocks = await _scheduleBlockRepository.GetByEmployeeAndDateRangeAsync(
                businessId,
                request.EmployeeId.Value,
                request.StartDate,
                request.EndDate,
                cancellationToken);
        }
        else
        {
            blocks = await _scheduleBlockRepository.GetByDateRangeAsync(
                businessId,
                request.StartDate,
                request.EndDate,
                cancellationToken);
        }

        var dtos = blocks.Select(b => new ScheduleBlockDto
        {
            Id = b.Id,
            BusinessId = b.BusinessId,
            EmployeeId = b.EmployeeId,
            StartDateTime = b.StartDateTime,
            EndDateTime = b.EndDateTime,
            Reason = b.Reason
        }).ToList();

        return Result<IReadOnlyList<ScheduleBlockDto>>.Ok(dtos);
    }
}
