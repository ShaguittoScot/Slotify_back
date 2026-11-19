using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Application.Features.SectorTemplates.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Businesses.Queries;

/// <summary>
/// Handler para consultar la lista de negocios para el explorador y catálogo de clientes.
/// Sigue Clean Architecture, SRP y Dependency Inversion (DIP) interactuando únicamente con abstracciones.
/// </summary>
public class GetBusinessesHandler(IBusinessRepository businessRepository)
    : IRequestHandler<GetBusinessesQuery, Result<IEnumerable<BusinessListItemDto>>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex NonAlphanumericRegex = new(
        @"[^a-z0-9]+",
        RegexOptions.Compiled
    );

    private readonly IBusinessRepository _businessRepository = businessRepository;

    public async Task<Result<IEnumerable<BusinessListItemDto>>> Handle(
        GetBusinessesQuery request,
        CancellationToken cancellationToken)
    {
        var businesses = await _businessRepository.GetAllWithDetailsAsync(cancellationToken);

        var dtos = businesses.Select(b =>
        {
            var customConfig = TryParseCustomFormConfig(b.CustomFormConfig);
            var customLogo = !string.IsNullOrWhiteSpace(customConfig?.LogoUrl)
                ? customConfig.LogoUrl
                : null;
            var customAccent = !string.IsNullOrWhiteSpace(customConfig?.AccentColor)
                ? customConfig.AccentColor
                : null;

            var logoUrl = !string.IsNullOrWhiteSpace(b.Brand?.LogoUrl)
                ? b.Brand.LogoUrl
                : customLogo;

            var brandColor = (!string.IsNullOrWhiteSpace(b.Brand?.PrimaryColor)
                              && (!b.Brand.IsDefault || b.Brand.PrimaryColor != "#000000"))
                ? b.Brand.PrimaryColor
                : customAccent;

            var categoryName = b.SectorTemplate?.Name;
            var category = !string.IsNullOrWhiteSpace(categoryName)
                ? categoryName
                : "Servicios Generales";
            var categoryKey = GenerateCategoryKey(categoryName);

            var slug = !string.IsNullOrWhiteSpace(b.Slug)
                ? b.Slug
                : (!string.IsNullOrWhiteSpace(b.Name) ? Business.GenerateSlug(b.Name) : string.Empty);

            var services = b.Services != null
                ? b.Services
                    .Where(s => s.IsActive && s.DeletedAt == null)
                    .Select(s => new BusinessServiceItemDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Price = s.Price,
                        DurationMinutes = s.DurationMinutes
                    })
                    .ToList()
                : [];

            return new BusinessListItemDto
            {
                Id = b.Id.ToString(),
                Name = b.Name,
                Slug = slug,
                Phone = b.Phone,
                Category = category,
                CategoryKey = categoryKey,
                TimeZone = b.TimeZone,
                MinAdvanceHours = b.MinAdvanceHours,
                MaxAdvanceDays = b.MaxAdvanceDays,
                SectorTemplateId = b.SectorTemplateId,
                LogoUrl = logoUrl,
                BrandColor = brandColor,
                Services = services
            };
        }).ToList();

        return Result<IEnumerable<BusinessListItemDto>>.Ok(dtos);
    }

    /// <summary>
    /// Genera una clave normalizada para filtros de categoría URL-friendly,
    /// eliminando acentos/diacríticos, convirtiendo a minúsculas y reemplazando espacios
    /// y caracteres especiales por guiones.
    /// </summary>
    public static string GenerateCategoryKey(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return "general";

        var normalized = categoryName.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var key = NonAlphanumericRegex.Replace(clean, "-").Trim('-');

        return string.IsNullOrEmpty(key) ? "general" : key;
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
