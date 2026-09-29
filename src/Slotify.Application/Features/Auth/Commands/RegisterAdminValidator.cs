using FluentValidation;

namespace Slotify.Application.Features.Auth.Commands;

/// <summary>
/// Validaciones para el comando de registro de administrador.
/// Relacionado con: US-000
/// 
/// TODO: Implementar reglas completas de validación.
/// </summary>
public class RegisterAdminValidator : AbstractValidator<RegisterAdminCommand>
{
    public RegisterAdminValidator()
    {
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

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de Supabase es requerido.");

        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("El nombre del negocio es requerido.")
            .MaximumLength(150).WithMessage("El nombre del negocio no puede exceder 150 caracteres.");
    }
}
