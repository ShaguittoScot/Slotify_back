namespace Slotify.Application.Features.Businesses.DTOs;

/// <summary>
/// DTO de solicitud para la actualización de datos generales y ubicación física
/// del establecimiento asociado al propietario autenticado.
/// </summary>
public record UpdateBusinessProfileRequest
{
    /// <summary>Nombre comercial del negocio.</summary>
    public required string Name { get; init; }

    /// <summary>Teléfono de contacto o atención.</summary>
    public string? Phone { get; init; }

    /// <summary>Descripción comercial o reseña del establecimiento.</summary>
    public string? Description { get; init; }

    /// <summary>Calle de la ubicación física.</summary>
    public string? Street { get; init; }

    /// <summary>Número exterior del establecimiento.</summary>
    public string? ExteriorNumber { get; init; }

    /// <summary>Número interior del establecimiento (opcional).</summary>
    public string? InteriorNumber { get; init; }

    /// <summary>Colonia o barrio.</summary>
    public string? Neighborhood { get; init; }

    /// <summary>Ciudad o municipio.</summary>
    public string? City { get; init; }

    /// <summary>Estado o provincia.</summary>
    public string? State { get; init; }

    /// <summary>Código postal.</summary>
    public string? PostalCode { get; init; }

    /// <summary>Código ISO de país (ej. "MX").</summary>
    public string? CountryCode { get; init; }
}
