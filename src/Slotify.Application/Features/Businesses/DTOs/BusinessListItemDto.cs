namespace Slotify.Application.Features.Businesses.DTOs;

/// <summary>
/// DTO para la lista de negocios en el explorador de clientes.
/// </summary>
public record BusinessListItemDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Phone { get; init; }
    public required string Category { get; init; }
    public required string CategoryKey { get; init; }
    public required string TimeZone { get; init; }
    public required int MinAdvanceHours { get; init; }
    public required int MaxAdvanceDays { get; init; }
    public int? SectorTemplateId { get; init; }
    public string? LogoUrl { get; init; }
    public string? BrandColor { get; init; }
    public List<BusinessServiceItemDto> Services { get; init; } = [];
}

/// <summary>
/// DTO con datos esenciales de un servicio para previsualización en tarjetas de negocios.
/// </summary>
public record BusinessServiceItemDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required int DurationMinutes { get; init; }
}
