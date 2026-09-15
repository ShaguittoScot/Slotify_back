using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.ScheduleBlocks.DTOs;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.ScheduleBlocks.Queries;

public class GetScheduleBlocksHandler(IScheduleBlockRepository scheduleBlockRepository)
    : IRequestHandler<GetScheduleBlocksQuery, Result<IReadOnlyList<ScheduleBlockDto>>>
{
    private readonly IScheduleBlockRepository _scheduleBlockRepository = scheduleBlockRepository;

    public async Task<Result<IReadOnlyList<ScheduleBlockDto>>> Handle(
        GetScheduleBlocksQuery request,
        CancellationToken cancellationToken)
    {
        var blocks = await _scheduleBlockRepository.GetByDateRangeAsync(
            request.BusinessId,
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
