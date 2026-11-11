using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Auth.Commands;

/// <summary>
/// Emite el JWT propio de Slotify (JwtService) con el claim businessId para un usuario autenticado en Supabase.
/// Relacionado con: US-022 (Endpoints protegidos con el negocio del token).
/// </summary>
public record ExchangeTokenCommand(Guid UserId, string Email) : IRequest<Result<AuthResponse>>;

public class ExchangeTokenHandler(
    IUserRepository userRepository,
    IJwtService jwtService) : IRequestHandler<ExchangeTokenCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IJwtService _jwtService = jwtService;

    public async Task<Result<AuthResponse>> Handle(
        ExchangeTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<AuthResponse>.Fail("Credenciales de sesión inválidas.");
        }

        var email = request.Email.Trim();
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            return Result<AuthResponse>.NotFound("El usuario aún no cuenta con un negocio sincronizado.");
        }

        if (!user.IsActive || user.BusinessId == Guid.Empty || user.Id != request.UserId ||
            !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return Result<AuthResponse>.Fail("No se pudo vincular esta cuenta con un negocio activo.", ErrorType.Forbidden);
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return Result<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.Add(IJwtService.AccessTokenLifetime),
            User = new AuthUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                BusinessId = user.BusinessId
            }
        });
    }
}

