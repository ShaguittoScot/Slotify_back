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
    IBusinessRepository businessRepository
) : IRequestHandler<CompleteBusinessOnboardingCommand, Result<AuthUserDto>>
{
    private readonly IBusinessOnboardingPersistence _persistence = persistence;
    private readonly IBusinessRepository _businessRepository = businessRepository;

    public async Task<Result<AuthUserDto>> Handle(
        CompleteBusinessOnboardingCommand request,
        CancellationToken cancellationToken)
    {
        Result<AuthUserDto>? result = null;

        await _persistence.ExecuteInTransactionAsync(
            async ct =>
            {
                var user = await _persistence.GetUserWithBusinessDetailsAsync(request.Id, ct);

                if (user is null)
                {
                    result = Result<AuthUserDto>.Fail(
                        "No existe un usuario registrado con el ID proporcionado.");
                    return;
                }

                if (!string.Equals(
                    user.Email.Trim(),
                    request.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    result = Result<AuthUserDto>.Fail(
                        "El correo electrónico no coincide con el usuario registrado.");
                    return;
                }

                if (user.Role != UserRole.Owner)
                {
                    result = Result<AuthUserDto>.Fail(
                        "El usuario no tiene permisos de administrador del negocio.");
                    return;
                }

                var business = user.Business;

                if (business is null)
                {
                    result = Result<AuthUserDto>.Fail("El usuario no tiene un negocio asociado.");
                    return;
                }

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
                            Role = "DUENO",
                            BusinessId = business.Id,
                        });
                    return;
                }

                var hasBusinessHours = await _persistence.HasBusinessHoursAsync(business.Id, ct);

                if (business.Brand is not null || business.Services.Any() || hasBusinessHours)
                {
                    result = Result<AuthUserDto>.Fail(
                        "El negocio contiene datos parciales de onboarding. Se requiere revisar su información antes de continuar.");
                    return;
                }

                var newSlug = Business.GenerateSlug(request.Business.Name);

                if (!string.Equals(newSlug, business.Slug, StringComparison.OrdinalIgnoreCase))
                {
                    var slugExists = await _businessRepository.SlugExistsAsync(newSlug, ct);

                    if (slugExists)
                    {
                        result = Result<AuthUserDto>.Fail(
                            "El nombre del negocio genera un slug que ya está en uso.");
                        return;
                    }
                }

                user.FirstName = request.FirstName.Trim();
                user.LastName = request.LastName.Trim();

                business.Name = request.Business.Name.Trim();
                business.Slug = newSlug;
                business.Phone = string.IsNullOrWhiteSpace(request.Business.Phone)
                    ? null
                    : request.Business.Phone.Trim();
                business.SectorTemplateId = request.Business.SectorTemplateId;
                business.TimeZone = request.Business.TimeZone.Trim();

                var brand = new BusinessBrand
                {
                    BusinessId = business.Id,
                    LogoUrl = null,
                    PrimaryColor = "#000000",
                    IsDefault = true,
                };

                _persistence.AddBusinessBrand(brand);

                var schedules = request.BusinessHours
                    .Select(hour => new AvailabilitySchedule
                    {
                        BusinessId = business.Id,
                        EmployeeId = null,
                        DayOfWeek = hour.DayOfWeek,
                        StartTime = hour.StartTime,
                        EndTime = hour.EndTime,
                    })
                    .ToList();

                _persistence.AddBusinessHours(schedules);

                var services = request.Services
                    .Select(service => new Service
                    {
                        BusinessId = business.Id,
                        Name = service.Name.Trim(),
                        DurationMinutes = service.DurationMinutes,
                        Price = service.Price,
                        IsActive = true,
                    })
                    .ToList();

                _persistence.AddServices(services);

                business.OnboardingCompletedAt = DateTime.UtcNow;

                await _persistence.SaveChangesAsync(ct);

                result = Result<AuthUserDto>.Ok(
                    new AuthUserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = "DUENO",
                        BusinessId = business.Id,
                    });
            },
            cancellationToken);

        return result ?? Result<AuthUserDto>.Fail("No fue posible completar el onboarding.");
    }
}
