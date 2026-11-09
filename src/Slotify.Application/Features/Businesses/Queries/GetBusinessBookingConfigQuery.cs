using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.SectorTemplates.DTOs;
using Slotify.Domain.Interfaces;
using System.Text.Json;

namespace Slotify.Application.Features.Businesses.Queries;

public record GetBusinessBookingConfigQuery(string Slug) : IRequest<Result<BookingFormConfigDto>>;

public class GetBusinessBookingConfigQueryHandler(
    IBusinessRepository businessRepository,
    ISectorTemplateRepository sectorTemplateRepository)
    : IRequestHandler<GetBusinessBookingConfigQuery, Result<BookingFormConfigDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly ISectorTemplateRepository _sectorTemplateRepository = sectorTemplateRepository;

    public async Task<Result<BookingFormConfigDto>> Handle(
        GetBusinessBookingConfigQuery request,
        CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (business == null)
            return Result<BookingFormConfigDto>.NotFound("Business not found");

        if (!string.IsNullOrWhiteSpace(business.CustomFormConfig))
        {
            try
            {
                var customConfig = JsonSerializer.Deserialize<BookingFormConfigDto>(
                    business.CustomFormConfig,
                    JsonOptions
                );
                if (customConfig != null)
                {
                    var enrichedCustomConfig = customConfig with
                    {
                        BusinessName = string.IsNullOrWhiteSpace(customConfig.BusinessName)
                            ? business.Name
                            : customConfig.BusinessName,
                        SecondaryColor = string.IsNullOrWhiteSpace(customConfig.SecondaryColor)
                            ? customConfig.AccentColor
                            : customConfig.SecondaryColor
                    };
                    return Result<BookingFormConfigDto>.Ok(enrichedCustomConfig);
                }
            }
            catch
            {
                /* Fallback a la plantilla */
            }
        }

        if (business.SectorTemplateId == null)
            return Result<BookingFormConfigDto>.NotFound("Business has no sector template assigned");

        var template = await _sectorTemplateRepository.GetByIdAsync(
            business.SectorTemplateId.Value,
            cancellationToken);
        if (template == null)
            return Result<BookingFormConfigDto>.NotFound("Sector template not found");

        try
        {
            var config = JsonSerializer.Deserialize<BookingFormConfigDto>(
                template.FormConfig,
                JsonOptions
            ) ?? new BookingFormConfigDto();

            var enrichedConfig = config with
            {
                BusinessName = string.IsNullOrWhiteSpace(config.BusinessName)
                    ? business.Name
                    : config.BusinessName
            };

            return Result<BookingFormConfigDto>.Ok(enrichedConfig);
        }
        catch
        {
            return Result<BookingFormConfigDto>.Ok(new BookingFormConfigDto
            {
                BusinessName = business.Name
            });
        }
    }
}
