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
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterAdminCommand, Result<AuthUserDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<AuthUserDto>> Handle(
        RegisterAdminCommand request,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            return Result<AuthUserDto>.Fail("El correo electrónico ya está registrado.");

        var businessSlug = request.BusinessName.ToLowerInvariant().Replace(" ", "-");
        if (await _businessRepository.SlugExistsAsync(businessSlug, cancellationToken))
            return Result<AuthUserDto>.Fail("El nombre del negocio ya está en uso.");

        var business = new Business
        {
            Name = request.BusinessName,
            Slug = businessSlug,
            Phone = request.BusinessPhone,
            SectorTemplateId = request.SectorTemplateId
        };

        var user = new User
        {
            Id = request.Id, // Usamos el ID generado por Supabase Auth
            FullName = request.FullName,
            Email = request.Email,
            Role = UserRole.Owner,
            Business = business
        };

        await _businessRepository.AddAsync(business, cancellationToken);
        await _userRepository.AddAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthUserDto>.Ok(new AuthUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            BusinessId = business.Id
        });
    }
}

