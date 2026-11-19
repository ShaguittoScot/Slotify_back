using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.DTOs;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Businesses.Commands;

/// <summary>
/// Handler CQRS para actualizar la información general y ubicación física del negocio del propietario autenticado.
/// Cumple con Single Responsibility (SRP) y Zero Trust:
/// 1. Extrae la identidad del usuario desde el contexto autenticado (ICurrentUserService).
/// 2. Valida estrictamente que el usuario tenga rol de propietario (Owner / DUENO).
/// 3. Resuelve el negocio únicamente mediante las relaciones persistidas en la base de datos (nunca desde el cliente).
/// 4. Actualiza exclusivamente los campos editables y preserva los campos del sistema protegidos (Slug, Id, etc.).
/// 5. Persiste a través del repositorio y Unit of Work.
/// </summary>
public class UpdateMyBusinessProfileHandler(
    IUserRepository userRepository,
    IBusinessRepository businessRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateMyBusinessProfileCommand, Result<BusinessProfileDto>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<BusinessProfileDto>> Handle(
        UpdateMyBusinessProfileCommand request,
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
                "Acceso denegado. Se requieren permisos de propietario para actualizar este perfil.",
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

        // 5. Actualización controlada de campos editables
        business.Name = request.Name.Trim();
        business.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        business.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        business.Street = string.IsNullOrWhiteSpace(request.Street) ? null : request.Street.Trim();
        business.ExteriorNumber = string.IsNullOrWhiteSpace(request.ExteriorNumber) ? null : request.ExteriorNumber.Trim();
        business.InteriorNumber = string.IsNullOrWhiteSpace(request.InteriorNumber) ? null : request.InteriorNumber.Trim();
        business.Neighborhood = string.IsNullOrWhiteSpace(request.Neighborhood) ? null : request.Neighborhood.Trim();
        business.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        business.State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim();
        business.PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
        business.CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim().ToUpperInvariant();
        business.UpdatedAt = DateTime.UtcNow;

        // 6. Persistencia
        await _businessRepository.UpdateAsync(business, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Mapeo a BusinessProfileDto
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
