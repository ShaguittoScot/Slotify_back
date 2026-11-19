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
    IBusinessRepository businessRepository,
    IJwtService jwtService) : IRequestHandler<ExchangeTokenCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly IJwtService _jwtService = jwtService;

    public async Task<Result<AuthResponse>> Handle(
        ExchangeTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<AuthResponse>.Fail("Credenciales de sesión inválidas.", ErrorType.Validation);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);

        if (user == null)
        {
            return Result<AuthResponse>.Fail("El usuario aún no cuenta con un negocio sincronizado.", ErrorType.NotFound);
        }

        // FASE 3: Verificación estricta de identificadores (Zero Trust)
        // No mutar ni vincular automáticamente cuentas con UUID diferente.
        if (user.Id != request.UserId)
        {
            return Result<AuthResponse>.Fail(
                $"Discrepancia de identidad: el ID del proveedor de autenticación ({request.UserId}) no coincide con el registrado en el sistema ({user.Id}) para este correo.",
                ErrorType.Conflict);
        }

        if (!string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Result<AuthResponse>.Fail("El correo electrónico no coincide con el usuario autenticado.", ErrorType.Forbidden);
        }

        if (user.BusinessId == Guid.Empty)
        {
            return Result<AuthResponse>.Fail("Inconsistencia de datos: el usuario no tiene un negocio asignado.", ErrorType.Conflict);
        }

        var business = await _businessRepository.GetByIdAsync(user.BusinessId, cancellationToken);
        var isOnboardingCompleted = business?.OnboardingCompletedAt.HasValue ?? false;

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return Result<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new AuthUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                BusinessId = user.BusinessId,
                IsOnboardingCompleted = isOnboardingCompleted
            }
        });
    }
}

