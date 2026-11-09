using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.ScheduleBlocks.DTOs;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Queries;

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

        var blocks = await _scheduleBlockRepository.GetByDateRangeAsync(
            businessId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        if (request.EmployeeId.HasValue)
        {
            blocks = blocks
                .Where(b => !b.EmployeeId.HasValue || b.EmployeeId == request.EmployeeId.Value)
                .ToList();
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
