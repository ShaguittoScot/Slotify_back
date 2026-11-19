using System.Text.Json;
using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Application.Features.SectorTemplates.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Businesses.Queries;

/// <summary>
/// Handler para consultar el perfil y catálogo detallado público de un negocio mediante su slug.
/// Cumple con Single Responsibility (SRP), Clean Architecture e Inversión de Dependencias (DIP).
/// </summary>
public class GetBusinessBySlugHandler(IBusinessRepository businessRepository)
    : IRequestHandler<GetBusinessBySlugQuery, Result<BusinessDetailDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBusinessRepository _businessRepository = businessRepository;

    public async Task<Result<BusinessDetailDto>> Handle(
        GetBusinessBySlugQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return Result<BusinessDetailDto>.NotFound("Negocio no encontrado o no disponible públicamente.");
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var business = await _businessRepository.GetBySlugWithDetailsAsync(normalizedSlug, cancellationToken);

        if (business == null)
        {
            return Result<BusinessDetailDto>.NotFound("Negocio no encontrado o no disponible públicamente.");
        }

        var customConfig = TryParseCustomFormConfig(business.CustomFormConfig);
        var customLogo = !string.IsNullOrWhiteSpace(customConfig?.LogoUrl)
            ? customConfig.LogoUrl
            : null;
        var customAccent = !string.IsNullOrWhiteSpace(customConfig?.AccentColor)
            ? customConfig.AccentColor
            : null;

        var logoUrl = !string.IsNullOrWhiteSpace(business.Brand?.LogoUrl)
            ? business.Brand.LogoUrl
            : customLogo;

        var brandColor = (!string.IsNullOrWhiteSpace(business.Brand?.PrimaryColor)
                          && (!business.Brand.IsDefault || business.Brand.PrimaryColor != "#000000"))
            ? business.Brand.PrimaryColor
            : customAccent;

        var categoryName = business.SectorTemplate?.Name;
        var category = !string.IsNullOrWhiteSpace(categoryName)
            ? categoryName
            : "Servicios Generales";
        var categoryKey = GetBusinessesHandler.GenerateCategoryKey(categoryName);

        var slug = !string.IsNullOrWhiteSpace(business.Slug)
            ? business.Slug
            : (!string.IsNullOrWhiteSpace(business.Name) ? Business.GenerateSlug(business.Name) : string.Empty);

        var services = business.Services != null
            ? business.Services
                .Where(s => s.IsActive && s.DeletedAt == null)
                .Select(s => new BusinessDetailServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = null,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    BufferTimeMinutes = s.BufferTimeMinutes,
                    IsActive = s.IsActive
                })
                .ToList()
            : [];

        var dto = new BusinessDetailDto
        {
            Id = business.Id.ToString(),
            Name = business.Name,
            Slug = slug,
            Phone = business.Phone,
            Category = category,
            CategoryKey = categoryKey,
            TimeZone = business.TimeZone,
            MinAdvanceHours = business.MinAdvanceHours,
            MaxAdvanceDays = business.MaxAdvanceDays,
            SectorTemplateId = business.SectorTemplateId,
            LogoUrl = logoUrl,
            BrandColor = brandColor,
            Services = services
        };

        return Result<BusinessDetailDto>.Ok(dto, "Negocio obtenido exitosamente");
    }

    private static BookingFormConfigDto? TryParseCustomFormConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<BookingFormConfigDto>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
