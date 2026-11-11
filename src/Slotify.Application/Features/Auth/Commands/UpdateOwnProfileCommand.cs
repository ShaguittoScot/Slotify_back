using FluentValidation;
using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Auth.Commands;

public record UpdateOwnProfileCommand(string FirstName, string LastName, string? BusinessName)
    : IRequest<Result<AuthUserDto>>;

public sealed class UpdateOwnProfileValidator : AbstractValidator<UpdateOwnProfileCommand>
{
    public UpdateOwnProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Ingresa tu nombre.")
            .MaximumLength(100).WithMessage("El nombre admite hasta 100 caracteres.");
        RuleFor(x => x.LastName).NotNull().MaximumLength(100)
            .WithMessage("Los apellidos admiten hasta 100 caracteres.");
        RuleFor(x => x.BusinessName).MaximumLength(150).WithMessage("El nombre comercial admite hasta 150 caracteres.");
        RuleFor(x => x.BusinessName).NotEmpty().WithMessage("Ingresa el nombre del negocio.")
            .When(x => x.BusinessName is not null);
    }
}

public sealed class UpdateOwnProfileHandler(
    ICurrentUserService currentUser, IUserRepository users, IBusinessRepository businesses, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateOwnProfileCommand, Result<AuthUserDto>>
{
    public async Task<Result<AuthUserDto>> Handle(UpdateOwnProfileCommand request, CancellationToken token)
    {
        if (currentUser.UserId is not { } userId || currentUser.BusinessId is not { } businessId)
            return Result<AuthUserDto>.Fail("La sesión no tiene un perfil de negocio.", ErrorType.Forbidden);

        var user = await users.GetByIdAsync(userId, token);
        if (user is null || !user.IsActive || user.BusinessId != businessId)
            return Result<AuthUserDto>.Fail("El perfil no pertenece a la sesión activa.", ErrorType.Forbidden);

        var business = await businesses.GetByIdAsync(businessId, token);
        if (business is null) return Result<AuthUserDto>.NotFound("No se encontró el negocio.");

        if (request.BusinessName is { } businessName && business.Name != businessName.Trim())
        {
            if (user.Role != UserRole.Owner)
                return Result<AuthUserDto>.Fail("Solo el propietario puede cambiar el nombre del negocio.", ErrorType.Forbidden);
            business.Name = businessName.Trim();
            await businesses.UpdateAsync(business, token);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        await users.UpdateAsync(user, token);
        await unitOfWork.SaveChangesAsync(token);
        return Result<AuthUserDto>.Ok(new AuthUserDto
        {
            Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, FullName = user.FullName,
            Email = user.Email, Role = user.Role.ToString(), BusinessId = business.Id, BusinessName = business.Name
        });
    }
}
