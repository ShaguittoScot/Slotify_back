using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Businesses.Queries;

public record BusinessHours(short DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public record OwnBusinessInfo(Guid Id, string Name, string Slug, string? Phone, string TimeZone, string? Sector, IReadOnlyList<BusinessHours> Hours);
public record GetOwnBusinessInfoQuery : IRequest<Result<OwnBusinessInfo>>;

public sealed class GetOwnBusinessInfoHandler(
    ICurrentUserService currentUser, IBusinessRepository businesses,
    ISectorTemplateRepository templates, IAvailabilityScheduleRepository schedules)
    : IRequestHandler<GetOwnBusinessInfoQuery, Result<OwnBusinessInfo>>
{
    public async Task<Result<OwnBusinessInfo>> Handle(GetOwnBusinessInfoQuery request, CancellationToken token)
    {
        if (currentUser.BusinessId is not { } businessId || businessId == Guid.Empty)
            return Result<OwnBusinessInfo>.Fail("La sesión no tiene un negocio asignado.", ErrorType.Forbidden);

        var business = await businesses.GetByIdAsync(businessId, token);
        if (business is null) return Result<OwnBusinessInfo>.NotFound("No se encontró el negocio.");
        var sector = business.SectorTemplateId is { } id ? await templates.GetByIdAsync(id, token) : null;
        var hours = (await schedules.GetByBusinessIdAsync(businessId, token))
            .Where(schedule => schedule.EmployeeId is null)
            .OrderBy(schedule => schedule.DayOfWeek).ThenBy(schedule => schedule.StartTime)
            .Select(schedule => new BusinessHours(schedule.DayOfWeek, schedule.StartTime, schedule.EndTime))
            .ToArray();
        return Result<OwnBusinessInfo>.Ok(new(
            business.Id, business.Name, business.Slug, business.Phone, business.TimeZone, sector?.Name, hours));
    }
}
