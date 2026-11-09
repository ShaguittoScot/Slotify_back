namespace Slotify.Application.Features.Businesses.DTOs;

/// <summary>
/// DTO de solicitud para actualizar la configuración de formulario de reserva de un negocio.
/// </summary>
public record UpdateFormConfigRequest
{
    public object FormConfig { get; init; } = new();
}

