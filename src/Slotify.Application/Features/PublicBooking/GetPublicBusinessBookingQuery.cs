using System.Text.Json;
using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.SectorTemplates.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.PublicBooking;

public record GetPublicBusinessBookingQuery(string Slug) : IRequest<Result<PublicBusinessBookingDto>>;

public sealed class GetPublicBusinessBookingHandler(
    IBusinessRepository businesses,
    IAvailabilityScheduleRepository schedules)
    : IRequestHandler<GetPublicBusinessBookingQuery, Result<PublicBusinessBookingDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<PublicBusinessBookingDto>> Handle(
        GetPublicBusinessBookingQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
            return Result<PublicBusinessBookingDto>.Fail("El identificador del negocio es obligatorio.", ErrorType.Validation);

        var business = await businesses.GetBySlugWithDetailsAsync(request.Slug, cancellationToken);
        if (business is null)
            return Result<PublicBusinessBookingDto>.NotFound("Negocio no encontrado.");

        var businessHours = PublicBookingHelpers.ExtractBusinessHours(
            await schedules.GetByBusinessIdAsync(business.Id, cancellationToken));

        var formConfig = ResolveFormConfig(business);
        var services = business.Services
            .Where(s => s.IsActive && s.DeletedAt is null)
            .OrderBy(s => s.Name)
            .Select(s => new PublicServiceDto(s.Id, s.Name, s.DurationMinutes, s.Price))
            .ToArray();

        return Result<PublicBusinessBookingDto>.Ok(new PublicBusinessBookingDto(
            business.Id,
            business.Name,
            business.Slug,
            business.Phone,
            business.TimeZone,
            business.SectorTemplate?.Name,
            business.Brand?.LogoUrl ?? formConfig?.LogoUrl,
            business.Brand?.PrimaryColor ?? formConfig?.AccentColor,
            business.MinAdvanceHours,
            business.MaxAdvanceDays,
            services,
            businessHours,
            formConfig));
    }

    private static BookingFormConfigDto? ResolveFormConfig(Business business)
    {
        var rawJson = !string.IsNullOrWhiteSpace(business.CustomFormConfig)
            ? business.CustomFormConfig
            : business.SectorTemplate?.FormConfig;

        BookingFormConfigDto? parsed = null;
        if (!string.IsNullOrWhiteSpace(rawJson))
        {
            try
            {
                parsed = JsonSerializer.Deserialize<BookingFormConfigDto>(rawJson, JsonOptions);
            }
            catch
            {
                parsed = null;
            }
        }

        if (parsed is null)
        {
            return new BookingFormConfigDto
            {
                BusinessName = business.Name,
                LogoUrl = business.Brand?.LogoUrl,
                AccentColor = !string.IsNullOrWhiteSpace(business.Brand?.PrimaryColor)
                    ? business.Brand!.PrimaryColor!
                    : "#6366F1",
            };
        }

        return parsed with
        {
            BusinessName = string.IsNullOrWhiteSpace(parsed.BusinessName) ? business.Name : parsed.BusinessName,
            LogoUrl = !string.IsNullOrWhiteSpace(parsed.LogoUrl) ? parsed.LogoUrl : business.Brand?.LogoUrl,
            AccentColor = !string.IsNullOrWhiteSpace(parsed.AccentColor)
                ? parsed.AccentColor
                : (!string.IsNullOrWhiteSpace(business.Brand?.PrimaryColor) ? business.Brand!.PrimaryColor! : "#6366F1"),
            SecondaryColor = !string.IsNullOrWhiteSpace(parsed.SecondaryColor)
                ? parsed.SecondaryColor
                : parsed.AccentColor,
        };
    }
}
