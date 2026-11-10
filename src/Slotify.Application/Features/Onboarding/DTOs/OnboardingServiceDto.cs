namespace Slotify.Application.Features.Onboarding.DTOs;

/// <summary>
/// Representa un servicio configurado por el negocio durante el onboarding.
/// </summary>
public record OnboardingServiceDto
{
    /// <summary>
    /// Nombre del servicio.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// DuraciÃ³n del servicio en minutos.
    /// </summary>
    public required int DurationMinutes { get; init; }

    /// <summary>
    /// Precio del servicio.
    /// </summary>
    public required decimal Price { get; init; }
}
