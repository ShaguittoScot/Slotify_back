namespace Slotify.Application.Features.Businesses.DTOs;

/// <summary>
/// DTO con la información completa del perfil y configuración del establecimiento
/// para consulta exclusiva del propietario autenticado.
/// </summary>
public record BusinessProfileDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? Phone { get; init; }
    public string? Description { get; init; }
    public string? Street { get; init; }
    public string? ExteriorNumber { get; init; }
    public string? InteriorNumber { get; init; }
    public string? Neighborhood { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
    public required string TimeZone { get; init; }
    public required int MinAdvanceHours { get; init; }
    public required int MaxAdvanceDays { get; init; }
    public int? SectorTemplateId { get; init; }
    public DateTime? OnboardingCompletedAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
