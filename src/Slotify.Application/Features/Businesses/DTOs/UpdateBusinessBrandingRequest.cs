namespace Slotify.Application.Features.Businesses.DTOs;

/// <summary>
/// DTO de solicitud para actualizar el logotipo y colores corporativos de un negocio sobre su formulario de reservas.
/// </summary>
public record UpdateBusinessBrandingRequest
{
    public string? LogoUrl { get; init; }
    public string AccentColor { get; init; } = "#6366F1";
    public string? SecondaryColor { get; init; }
    public string? BusinessName { get; init; }
}

