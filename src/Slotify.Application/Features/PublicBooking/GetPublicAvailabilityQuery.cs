using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.PublicBooking;

public record GetPublicAvailabilityQuery(string Slug, DateOnly Date) : IRequest<Result<PublicAvailabilityDto>>;

public sealed class GetPublicAvailabilityHandler(
    IBusinessRepository businesses,
    IAvailabilityScheduleRepository schedules,
    IAppointmentRepository appointments,
    IScheduleBlockRepository blocks)
    : IRequestHandler<GetPublicAvailabilityQuery, Result<PublicAvailabilityDto>>
{
    public async Task<Result<PublicAvailabilityDto>> Handle(
        GetPublicAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
            return Result<PublicAvailabilityDto>.Fail("El identificador del negocio es obligatorio.", ErrorType.Validation);

        var business = await businesses.GetBySlugAsync(request.Slug.Trim(), cancellationToken);
        if (business is null)
            return Result<PublicAvailabilityDto>.NotFound("Negocio no encontrado.");

        var tz = PublicBookingHelpers.ResolveTimeZone(business.TimeZone);
        var localStart = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = request.Date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);

        var businessHours = PublicBookingHelpers.ExtractBusinessHours(
            await schedules.GetByBusinessIdAsync(business.Id, cancellationToken));

        var dayAppointments = await appointments.GetByDateRangeAsync(business.Id, startUtc, endUtc, cancellationToken);
        var dayBlocks = await blocks.GetByDateRangeAsync(business.Id, startUtc, endUtc, cancellationToken);

        var occupied = dayAppointments
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Select(a => new OccupiedIntervalDto(a.StartTime, a.EndTime))
            .Concat(dayBlocks.Select(b => new OccupiedIntervalDto(b.StartDateTime, b.EndDateTime)))
            .OrderBy(i => i.StartTime)
            .ToArray();

        return Result<PublicAvailabilityDto>.Ok(new PublicAvailabilityDto(
            business.Slug,
            request.Date.ToString("yyyy-MM-dd"),
            business.TimeZone,
            businessHours,
            occupied));
    }
}
