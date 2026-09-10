using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Auth.Commands;

public class RegisterAdminHandler(
    IUserRepository userRepository,
    IBusinessRepository businessRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IUserSessionRepository userSessionRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterAdminCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<AuthResponse>> Handle(
        RegisterAdminCommand request,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            return Result<AuthResponse>.Fail("El correo electrónico ya está registrado.");

        var businessSlug = request.BusinessName.ToLowerInvariant().Replace(" ", "-");
        if (await _businessRepository.SlugExistsAsync(businessSlug, cancellationToken))
            return Result<AuthResponse>.Fail("El nombre del negocio ya está en uso.");

        var business = new Business
        {
            Name = request.BusinessName,
            Slug = businessSlug,
            Phone = request.BusinessPhone,
            SectorTemplateId = request.SectorTemplateId
        };

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Owner,
            Business = business
        };

        await _businessRepository.AddAsync(business, cancellationToken);
        await _userRepository.AddAsync(user, cancellationToken);

        var refreshToken = _jwtService.GenerateRefreshToken();
        var session = new UserSession
        {
            User = user,
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
                BusinessId = business.Id
            }
        });
    }
}

