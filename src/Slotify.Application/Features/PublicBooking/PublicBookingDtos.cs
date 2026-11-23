using Slotify.Application.Features.Businesses.Queries;
using Slotify.Application.Features.SectorTemplates.DTOs;
using Slotify.Domain.Entities;

namespace Slotify.Application.Features.PublicBooking;

public record PublicServiceDto(Guid Id, string Name, int DurationMinutes, decimal Price);

public record PublicBusinessBookingDto(
    Guid Id,
    string Name,
    string Slug,
    string? Phone,
    string TimeZone,
    string? Sector,
    string? LogoUrl,
    string? BrandColor,
    int MinAdvanceHours,
    int MaxAdvanceDays,
    IReadOnlyList<PublicServiceDto> Services,
    IReadOnlyList<BusinessHours> Hours,
    BookingFormConfigDto? FormConfig);

public record OccupiedIntervalDto(DateTime StartTime, DateTime EndTime);

public record PublicAvailabilityDto(
    string Slug,
    string Date,
    string TimeZone,
    IReadOnlyList<BusinessHours> Hours,
    IReadOnlyList<OccupiedIntervalDto> Occupied);

public record PublicAppointmentConfirmationDto(
    Guid Id,
    string BusinessName,
    string BusinessSlug,
    DateTime StartTime,
    DateTime EndTime,
    string ClientName,
    decimal AgreedTotal,
    string Status,
    string CancellationToken,
    bool IsRegisteredClient = false);

public record ConsumerAppointmentDto(
    Guid Id,
    string BusinessName,
    string BusinessSlug,
    string Title,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    decimal AgreedTotal);

internal static class PublicBookingHelpers
{
    public static BusinessHours[] ExtractBusinessHours(IEnumerable<AvailabilitySchedule> schedules) =>
        schedules
            .Where(s => s.EmployeeId is null)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new BusinessHours(s.DayOfWeek, s.StartTime, s.EndTime))
            .ToArray();

    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
