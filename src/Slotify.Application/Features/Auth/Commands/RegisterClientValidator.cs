namespace Slotify.Application.Features.Auth.Commands;

using FluentValidation;

public class RegisterClientValidator : AbstractValidator<RegisterClientCommand>
{
    public RegisterClientValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de Supabase es requerido.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.FirstName) || !string.IsNullOrWhiteSpace(x.FullName))
            .WithMessage("El nombre es requerido.");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es requerido.")
            .EmailAddress().WithMessage("El formato del correo no es válido.")
            .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("El teléfono no puede exceder 30 caracteres.");
    }
}
