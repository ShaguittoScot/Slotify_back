using FluentValidation;

namespace Slotify.Application.Features.Businesses.Commands;

/// <summary>
/// Validador de reglas de negocio para la actualización del perfil del establecimiento.
/// </summary>
public class UpdateMyBusinessProfileValidator : AbstractValidator<UpdateMyBusinessProfileCommand>
{
    public UpdateMyBusinessProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del negocio es requerido.")
            .MaximumLength(150).WithMessage("El nombre del negocio no puede exceder 150 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres.")
            .Matches(@"^\+?[0-9\s\-\(\)\.]{7,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("El formato del teléfono no es válido.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres.");

        RuleFor(x => x.Street)
            .MaximumLength(150).WithMessage("La calle no puede exceder 150 caracteres.");

        RuleFor(x => x.ExteriorNumber)
            .MaximumLength(20).WithMessage("El número exterior no puede exceder 20 caracteres.");

        RuleFor(x => x.InteriorNumber)
            .MaximumLength(20).WithMessage("El número interior no puede exceder 20 caracteres.");

        RuleFor(x => x.Neighborhood)
            .MaximumLength(100).WithMessage("La colonia no puede exceder 100 caracteres.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("La ciudad no puede exceder 100 caracteres.");

        RuleFor(x => x.State)
            .MaximumLength(100).WithMessage("El estado no puede exceder 100 caracteres.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(10).WithMessage("El código postal no puede exceder 10 caracteres.");

        RuleFor(x => x.CountryCode)
            .Length(2).WithMessage("El código de país debe constar de exactamente 2 letras (ej. MX).")
            .Matches(@"^[a-zA-Z]{2}$").WithMessage("El código de país solo puede contener caracteres alfabéticos.")
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
    }
}
