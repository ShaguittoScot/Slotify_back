namespace Slotify.Application.Features.Onboarding.DTOs;

/// <summary>
/// Datos del negocio capturados durante el onboarding.
/// </summary>
public record OnboardingBusinessDto
{
    public required string Name { get; init; }

    public string? Phone { get; init; }

    public required int SectorTemplateId { get; init; }

    public required string TimeZone { get; init; }
}
