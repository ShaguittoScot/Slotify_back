using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.ScheduleBlocks.DTOs;

namespace Slotify.Application.Features.ScheduleBlocks.Queries;

public record GetScheduleBlocksQuery : IRequest<Result<IReadOnlyList<ScheduleBlockDto>>>
{
    public required Guid BusinessId { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public Guid? EmployeeId { get; init; }
}
