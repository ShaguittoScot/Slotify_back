using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Onboarding.Commands;

public class CompleteBusinessOnboardingHandler(
    IBusinessOnboardingPersistence persistence,
    IBusinessRepository businessRepository,
    ICurrentUserService currentUserService
) : IRequestHandler<CompleteBusinessOnboardingCommand, Result<AuthUserDto>>
{
    private readonly IBusinessOnboardingPersistence _persistence = persistence;
    private readonly IBusinessRepository _businessRepository = businessRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<AuthUserDto>> Handle(
        CompleteBusinessOnboardingCommand request,
        CancellationToken cancellationToken
    )
    {
        // 1. Verificación de identidad autenticada
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null || _currentUserService.UserId == Guid.Empty)
        {
            return Result<AuthUserDto>.Fail(
                "No se pudo determinar la identidad del usuario autenticado.",
                ErrorType.Forbidden
            );
        }

        var currentUserId = _currentUserService.UserId.Value;

        Result<AuthUserDto>? result = null;

        await _persistence.ExecuteInTransactionAsync(
            async ct =>
            {
                // 2. Consulta del usuario autenticado en base de datos
                var user = await _persistence.GetUserWithBusinessDetailsAsync(currentUserId, ct);

                if (user is null)
                {
                    result = Result<AuthUserDto>.Fail(
                        "No existe un usuario registrado correspondiente a la sesión autenticada.",
                        ErrorType.NotFound
                    );
                    return;
                }

                // 3. Normalización y verificación de rol (dominio y claim del token)
                var isTokenAdmin = string.Equals(_currentUserService.Role, nameof(UserRole.Owner), StringComparison.OrdinalIgnoreCase);
                if (user.Role != UserRole.Owner || !isTokenAdmin)
                {
                    result = Result<AuthUserDto>.Fail(
                        "El usuario no tiene permisos de administrador del negocio.",
                        ErrorType.Forbidden
                    );
                    return;
                }

                // 4. Verificación del negocio asociado al usuario
                var business = user.Business;

                if (business is null)
                {
                    result = Result<AuthUserDto>.Fail(
                        "El usuario no tiene un negocio asociado.",
                        ErrorType.NotFound
                    );
                    return;
                }

                // 5. Aislamiento multi-inquilino: contrastar claim businessId con el negocio del usuario
                var tokenBusinessId = _currentUserService.BusinessId;
                if (tokenBusinessId is null || tokenBusinessId == Guid.Empty)
                {
                    result = Result<AuthUserDto>.Fail(
                        "El token no contiene un identificador de negocio válido.",
                        ErrorType.Forbidden
                    );
                    return;
                }

                if (tokenBusinessId.Value != business.Id || tokenBusinessId.Value != user.BusinessId)
                {
                    result = Result<AuthUserDto>.Fail(
                        "El negocio del token no coincide con el asignado al usuario.",
                        ErrorType.Forbidden
                    );
                    return;
                }

                // 6. Idempotencia: si ya se completó, responder exitosamente sin modificar datos
                if (business.OnboardingCompletedAt.HasValue)
                {
                    result = Result<AuthUserDto>.Ok(
                        new AuthUserDto
                        {
                            Id = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            FullName = user.FullName,
                            Email = user.Email,
                            Role = user.Role.ToString(),
                            BusinessId = business.Id,
                            IsOnboardingCompleted = true,
                        }
                    );

                    return;
                }

                // 7. Evitar duplicar configuraciones parciales
                var hasBusinessHours = await _persistence.HasBusinessHoursAsync(business.Id, ct);

                if (business.Brand is not null || business.Services.Any() || hasBusinessHours)
                {
                    result = Result<AuthUserDto>.Fail(
                        "El negocio contiene datos parciales de onboarding. Se requiere revisar su información antes de continuar."
                    );
                    return;
                }

                // 8. Generación de slug usando la lógica centralizada de la entidad
                var newSlug = Business.GenerateSlug(request.Business.Name);

                if (!string.Equals(newSlug, business.Slug, StringComparison.OrdinalIgnoreCase))
                {
                    var slugExists = await _businessRepository.SlugExistsAsync(newSlug, ct);

                    if (slugExists)
                    {
                        result = Result<AuthUserDto>.Fail(
                            "El nombre del negocio genera un slug que ya está en uso.",
                            ErrorType.Conflict
                        );
                        return;
                    }
                }

                // 9. Actualizar datos del administrador
                user.FirstName = request.FirstName.Trim();
                user.LastName = request.LastName.Trim();

                // 10. Actualizar datos del negocio existente
                business.Name = request.Business.Name.Trim();
                business.Slug = newSlug;
                business.Phone = string.IsNullOrWhiteSpace(request.Business.Phone)
                    ? null
                    : request.Business.Phone.Trim();
                business.SectorTemplateId = request.Business.SectorTemplateId;
                business.TimeZone = request.Business.TimeZone.Trim();

                // 11. Crear la marca predeterminada
                var brand = new BusinessBrand
                {
                    BusinessId = business.Id,
                    LogoUrl = null,
                    PrimaryColor = "#000000",
                    IsDefault = true,
                };

                _persistence.AddBusinessBrand(brand);

                // 12. Crear horarios generales del negocio
                var schedules = request
                    .BusinessHours.Select(hour => new AvailabilitySchedule
                    {
                        BusinessId = business.Id,
                        EmployeeId = null,
                        DayOfWeek = hour.DayOfWeek,
                        StartTime = hour.StartTime,
                        EndTime = hour.EndTime,
                    })
                    .ToList();

                _persistence.AddBusinessHours(schedules);

                // 13. Crear los servicios recibidos
                var services = request
                    .Services.Select(service => new Service
                    {
                        BusinessId = business.Id,
                        Name = service.Name.Trim(),
                        DurationMinutes = service.DurationMinutes,
                        Price = service.Price,
                        IsActive = true,
                    })
                    .ToList();

                _persistence.AddServices(services);

                // 14. Marcar el onboarding como completado
                business.OnboardingCompletedAt = DateTime.UtcNow;

                // 15. Persistir todo atómicamente dentro de la transacción
                await _persistence.SaveChangesAsync(ct);

                result = Result<AuthUserDto>.Ok(
                    new AuthUserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = user.Role.ToString(),
                        BusinessId = business.Id,
                        IsOnboardingCompleted = true,
                    }
                );
            },
            cancellationToken
        );

        return result ?? Result<AuthUserDto>.Fail("No fue posible completar el onboarding.");
    }
}
