using MediatR;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Businesses.Queries;

/// <summary>
/// Handler para consultar la lista de negocios para el explorador y catálogo de clientes.
/// Sigue Clean Architecture, SRP y Dependency Inversion (DIP) interactuando únicamente con abstracciones.
/// </summary>
public class GetBusinessesHandler(IBusinessRepository businessRepository)
    : IRequestHandler<GetBusinessesQuery, IEnumerable<BusinessListItemDto>>
{
    private readonly IBusinessRepository _businessRepository = businessRepository;

    public async Task<IEnumerable<BusinessListItemDto>> Handle(
        GetBusinessesQuery request,
        CancellationToken cancellationToken)
    {
        var businesses = await _businessRepository.GetAllWithDetailsAsync(cancellationToken);

        return businesses.Select(b => new BusinessListItemDto
        {
            Id = b.Id.ToString(),
            Name = b.Name,
            Slug = !string.IsNullOrWhiteSpace(b.Slug)
                ? b.Slug
                : b.Name.Trim().ToLowerInvariant().Replace(' ', '-'),
            Phone = b.Phone,
            Category = b.SectorTemplate?.Name ?? "Servicios Generales",
            CategoryKey = b.SectorTemplate?.Name?.ToLowerInvariant() ?? "general",
            TimeZone = b.TimeZone,
            MinAdvanceHours = b.MinAdvanceHours,
            MaxAdvanceDays = b.MaxAdvanceDays,
            SectorTemplateId = b.SectorTemplateId,
            LogoUrl = b.Brand?.LogoUrl,
            BrandColor = b.Brand?.PrimaryColor,
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
        });
    }
}

