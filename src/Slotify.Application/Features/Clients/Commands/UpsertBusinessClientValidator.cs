using FluentValidation;

namespace Slotify.Application.Features.Clients.Commands;

/// <summary>
/// Validador FluentValidation para UpsertBusinessClientCommand.
/// Relacionado con: US-026 (Construcción de módulo de clientes).
/// </summary>
public class UpsertBusinessClientValidator : AbstractValidator<UpsertBusinessClientCommand>
{
    public UpsertBusinessClientValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre del cliente es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres.");

        RuleFor(x => x.Email)
            .MaximumLength(150).WithMessage("El correo electrónico no puede exceder 150 caracteres.")
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("El formato del correo electrónico no es válido.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("El teléfono no puede exceder 30 caracteres.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes is not null)
            .WithMessage("Las notas no pueden exceder 1000 caracteres.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Debe proporcionar al menos un medio de contacto (correo electrónico o teléfono).");
    }
}
