using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.ScheduleBlocks.DTOs;

namespace Slotify.Application.Features.ScheduleBlocks.Queries;

/// <summary>
/// Query para listar bloqueos de agenda del negocio autenticado (US-022).
/// </summary>
public record GetScheduleBlocksQuery : IRequest<Result<IReadOnlyList<ScheduleBlockDto>>>
{
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public Guid? EmployeeId { get; init; }
}
