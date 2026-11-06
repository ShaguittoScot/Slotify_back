namespace Slotify.Application.Features.Businesses.DTOs;

/// <summary>
/// DTO de solicitud para cambiar la plantilla de sector de un negocio (US-013).
/// </summary>
public record ChangeTemplateRequest
{
    public int NewTemplateId { get; init; }
}

