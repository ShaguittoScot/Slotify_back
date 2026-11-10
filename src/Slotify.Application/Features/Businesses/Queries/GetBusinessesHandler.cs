using MediatR;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Application.Features.SectorTemplates.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using System.Text.Json;

namespace Slotify.Application.Features.Businesses.Queries;

/// <summary>
/// Handler para consultar la lista de negocios para el explorador y catálogo de clientes.
/// Sigue Clean Architecture, SRP y Dependency Inversion (DIP) interactuando únicamente con abstracciones.
/// </summary>
public class GetBusinessesHandler(IBusinessRepository businessRepository)
    : IRequestHandler<GetBusinessesQuery, IEnumerable<BusinessListItemDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBusinessRepository _businessRepository = businessRepository;

    public async Task<IEnumerable<BusinessListItemDto>> Handle(
        GetBusinessesQuery request,
        CancellationToken cancellationToken)
    {
        var businesses = await _businessRepository.GetAllWithDetailsAsync(cancellationToken);

        return businesses.Select(b =>
        {
            var customConfig = TryParseCustomFormConfig(b.CustomFormConfig);
            var logoUrl = !string.IsNullOrWhiteSpace(b.Brand?.LogoUrl)
                ? b.Brand.LogoUrl
                : customConfig?.LogoUrl;
            var brandColor = !string.IsNullOrWhiteSpace(b.Brand?.PrimaryColor) && b.Brand.PrimaryColor != "#000000"
                ? b.Brand.PrimaryColor
                : customConfig?.AccentColor;

            return new BusinessListItemDto
            {
                Id = b.Id.ToString(),
                Name = b.Name,
                Slug = !string.IsNullOrWhiteSpace(b.Slug)
                    ? b.Slug
                    : Business.GenerateSlug(b.Name),
                Phone = b.Phone,
                Category = b.SectorTemplate?.Name ?? "Servicios Generales",
                CategoryKey = b.SectorTemplate?.Name?.ToLowerInvariant() ?? "general",
                TimeZone = b.TimeZone,
                MinAdvanceHours = b.MinAdvanceHours,
                MaxAdvanceDays = b.MaxAdvanceDays,
                SectorTemplateId = b.SectorTemplateId,
                LogoUrl = logoUrl,
                BrandColor = brandColor,
                Services = b.Services
                    .Where(s => s.IsActive && s.DeletedAt == null)
                    .Select(s => new BusinessServiceItemDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Price = s.Price,
                        DurationMinutes = s.DurationMinutes
                    })
                    .ToList()
            };
        });
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
