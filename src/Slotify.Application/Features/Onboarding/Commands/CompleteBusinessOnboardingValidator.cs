using FluentValidation;
using Slotify.Domain.Interfaces;

namespace Slotify.Application.Features.Onboarding.Commands;

public class CompleteBusinessOnboardingValidator
    : AbstractValidator<CompleteBusinessOnboardingCommand>
{
    public CompleteBusinessOnboardingValidator(
        ISectorTemplateRepository sectorTemplateRepository)
    {
        // Datos del propietario
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("El nombre del propietario es requerido.")
            .MaximumLength(100)
            .WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .WithMessage("El apellido no puede exceder 100 caracteres.");

        // Datos del negocio
        RuleFor(x => x.Business)
            .NotNull()
            .WithMessage("Los datos del negocio son requeridos.");

        When(x => x.Business is not null, () =>
        {
            RuleFor(x => x.Business.Name)
                .NotEmpty()
                .WithMessage("El nombre del negocio es requerido.")
                .MaximumLength(150)
                .WithMessage("El nombre no puede exceder 150 caracteres.");

            RuleFor(x => x.Business.Phone)
                .MaximumLength(20)
                .WithMessage("El teléfono no puede exceder 20 caracteres.");

            RuleFor(x => x.Business.SectorTemplateId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage("Debe seleccionar un giro comercial válido.")
                .MustAsync(async (sectorId, cancellationToken) =>
                {
                    var sector = await sectorTemplateRepository.GetByIdAsync(
                        sectorId,
                        cancellationToken);
                    return sector is not null;
                })
                .WithMessage("El giro comercial seleccionado no existe.");

            RuleFor(x => x.Business.TimeZone)
                .NotEmpty()
                .WithMessage("La zona horaria es requerida.")
                .MaximumLength(50)
                .WithMessage("La zona horaria no puede exceder 50 caracteres.")
                .Must(BeValidTimeZone)
                .WithMessage("La zona horaria no es válida.");
        });

        // Horarios del negocio
        RuleFor(x => x.BusinessHours)
            .NotNull()
            .WithMessage("Los horarios son requeridos.")
            .NotEmpty()
            .WithMessage("Debe configurar al menos un horario de atención.");

        RuleForEach(x => x.BusinessHours)
            .ChildRules(hour =>
            {
                hour.RuleFor(x => x.DayOfWeek)
                    .InclusiveBetween((short)0, (short)6)
                    .WithMessage("El día debe estar entre 0 (domingo) y 6 (sábado).");

                hour.RuleFor(x => x.EndTime)
                    .GreaterThan(x => x.StartTime)
                    .WithMessage("La hora de fin debe ser posterior a la de inicio.");
            });

        RuleFor(x => x.BusinessHours)
            .Must(hours => hours is null ||
                hours.Select(h => h.DayOfWeek).Distinct().Count() == hours.Count)
            .WithMessage("No puede haber horarios duplicados para un mismo día.");

        // Servicios
        RuleFor(x => x.Services)
            .NotNull()
            .WithMessage("La lista de servicios es requerida.")
            .NotEmpty()
            .WithMessage("Debe registrar al menos un servicio inicial.");

        RuleForEach(x => x.Services)
            .ChildRules(service =>
            {
                service.RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("El nombre del servicio es requerido.")
                    .MaximumLength(150)
                    .WithMessage("El nombre no puede exceder 150 caracteres.");

                service.RuleFor(x => x.DurationMinutes)
                    .GreaterThan(0)
                    .WithMessage("La duración debe ser mayor que cero.");

                service.RuleFor(x => x.Price)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("El precio no puede ser negativo.")
                    .LessThanOrEqualTo(99999999.99m)
                    .WithMessage("El precio excede el máximo permitido.");
            });
    }

    private static bool BeValidTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
