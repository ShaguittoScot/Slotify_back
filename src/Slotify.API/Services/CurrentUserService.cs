using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Slotify.Application.Common.Interfaces;

namespace Slotify.API.Services;

/// <summary>
/// Implementación HTTP de ICurrentUserService que extrae los claims del JWT activo.
/// Relacionado con: US-022 (Endpoints protegidos con el negocio del token).
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
        }
    }

    public Guid? BusinessId
    {
        get
        {
            var raw = User?.FindFirstValue("businessId")
                   ?? User?.FindFirstValue("business_id");

            return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
        }
    }

    public string? Role =>
        User?.FindFirstValue(ClaimTypes.Role)
        ?? User?.FindFirstValue("role");
}

