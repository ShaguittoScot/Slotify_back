namespace Slotify.Application.Common.Interfaces;

/// <summary>
/// Abstracción para obtener la información del usuario autenticado desde el token JWT actual.
/// Relacionado con: US-022 (Endpoints protegidos con el negocio del token).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>ID del usuario autenticado (claim sub / NameIdentifier).</summary>
    Guid? UserId { get; }

    /// <summary>ID del negocio asociado al usuario autenticado (claim businessId).</summary>
    Guid? BusinessId { get; }

    /// <summary>Rol del usuario autenticado (claim role).</summary>
    string? Role { get; }

    /// <summary>Indica si la petición actual cuenta con un usuario autenticado válido.</summary>
    bool IsAuthenticated { get; }
}

