using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Auth.Commands;

public class LoginHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IUserSessionRepository userSessionRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Fail("Credenciales inválidas.");
        }

        if (!user.IsActive)
        {
            return Result<AuthResponse>.Fail("La cuenta está desactivada.");
        }

        user.LastLogin = DateTime.UtcNow;

        var refreshToken = _jwtService.GenerateRefreshToken();
        var session = new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = _passwordHasher.Hash(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _userSessionRepository.CreateAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user);

        return Result<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new AuthUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                BusinessId = user.BusinessId
            }
        });
    }
}

