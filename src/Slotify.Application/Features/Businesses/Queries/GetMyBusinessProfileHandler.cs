using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Businesses.Queries;

/// <summary>
/// Handler CQRS para consultar el perfil del negocio del propietario autenticado.
/// Cumple con Single Responsibility (SRP) y Zero Trust:
/// 1. Extrae la identidad del usuario desde el contexto autenticado (ICurrentUserService).
/// 2. Valida estrictamente que el usuario tenga rol de propietario (Owner / DUENO).
/// 3. Resuelve el negocio únicamente mediante las relaciones persistidas en la base de datos.
/// </summary>
public class GetMyBusinessProfileHandler(
    IUserRepository userRepository,
    IBusinessRepository businessRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetMyBusinessProfileQuery, Result<BusinessProfileDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<BusinessProfileDto>> Handle(
        GetMyBusinessProfileQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verificación de identidad autenticada
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null || _currentUserService.UserId == Guid.Empty)
        {
            return Result<BusinessProfileDto>.Fail(
                "No se pudo determinar la identidad del usuario autenticado.",
                ErrorType.Forbidden
            );
        }

        var currentUserId = _currentUserService.UserId.Value;

        // 2. Validación del claim de rol en el token
        var isTokenOwner = string.Equals(_currentUserService.Role, nameof(UserRole.Owner), StringComparison.OrdinalIgnoreCase)
                        || string.Equals(_currentUserService.Role, "DUENO", StringComparison.OrdinalIgnoreCase);

        if (!isTokenOwner)
        {
            return Result<BusinessProfileDto>.Fail(
                "Acceso denegado. Se requieren permisos de propietario para consultar este perfil.",
                ErrorType.Forbidden
            );
        }

        // 3. Verificación de identidad y rol en la base de datos (Zero Trust)
        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user is null)
        {
            return Result<BusinessProfileDto>.NotFound(
                "No existe un usuario registrado correspondiente a la sesión activa."
            );
        }

        if (user.Role != UserRole.Owner)
        {
            return Result<BusinessProfileDto>.Fail(
                "El usuario no tiene asignado el rol de propietario en el sistema.",
                ErrorType.Forbidden
            );
        }

        // 4. Resolución del negocio mediante las relaciones persistidas en la base de datos
        var business = await _businessRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        if (business is null)
        {
            return Result<BusinessProfileDto>.NotFound(
                "No existe un negocio asociado al propietario autenticado."
            );
        }

        // 5. Mapeo al DTO de perfil del negocio
        var dto = new BusinessProfileDto
        {
            Id = business.Id,
            Name = business.Name,
            Slug = business.Slug,
            Phone = business.Phone,
            Description = business.Description,
            Street = business.Street,
            ExteriorNumber = business.ExteriorNumber,
            InteriorNumber = business.InteriorNumber,
            Neighborhood = business.Neighborhood,
            City = business.City,
            State = business.State,
            PostalCode = business.PostalCode,
            CountryCode = business.CountryCode,
            TimeZone = business.TimeZone,
            MinAdvanceHours = business.MinAdvanceHours,
            MaxAdvanceDays = business.MaxAdvanceDays,
            SectorTemplateId = business.SectorTemplateId,
            OnboardingCompletedAt = business.OnboardingCompletedAt,
            CreatedAt = business.CreatedAt,
            UpdatedAt = business.UpdatedAt
        };

        return Result<BusinessProfileDto>.Ok(dto);
    }
}
