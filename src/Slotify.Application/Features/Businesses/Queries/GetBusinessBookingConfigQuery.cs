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
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly ISectorTemplateRepository _sectorTemplateRepository = sectorTemplateRepository;

    public async Task<Result<BookingFormConfigDto>> Handle(GetBusinessBookingConfigQuery request, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (business == null)
            return Result<BookingFormConfigDto>.Fail("Business not found");

        if (!string.IsNullOrWhiteSpace(business.CustomFormConfig))
        {
            try
            {
                var customConfig = JsonSerializer.Deserialize<BookingFormConfigDto>(
                    business.CustomFormConfig,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                if (customConfig != null)
                    return Result<BookingFormConfigDto>.Ok(customConfig);
            }
            catch { /* Fallback a la plantilla */ }
        }

        if (business.SectorTemplateId == null)
            return Result<BookingFormConfigDto>.Fail("Business has no sector template assigned");

        var template = await _sectorTemplateRepository.GetByIdAsync(business.SectorTemplateId.Value, cancellationToken);
        if (template == null)
            return Result<BookingFormConfigDto>.Fail("Sector template not found");

        try
        {
            var config = JsonSerializer.Deserialize<BookingFormConfigDto>(
                template.FormConfig,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return Result<BookingFormConfigDto>.Ok(config ?? new BookingFormConfigDto());
        }
        catch
        {
            return Result<BookingFormConfigDto>.Ok(new BookingFormConfigDto());
        }
    }
}
