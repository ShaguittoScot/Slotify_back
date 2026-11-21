namespace Slotify.Application.Features.Businesses.DTOs;

/// <summary>
/// DTO con el detalle público completo de un negocio para la pantalla de perfil y reserva de clientes.
/// </summary>
public record BusinessDetailDto
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
    public List<BusinessDetailServiceDto> Services { get; init; } = [];
}

/// <summary>
/// DTO de un servicio dentro del catálogo detallado del negocio.
/// </summary>
public record BusinessDetailServiceDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; } = null;
    public required decimal Price { get; init; }
    public required int DurationMinutes { get; init; }
    public required int BufferTimeMinutes { get; init; }
    public required bool IsActive { get; init; }
}
