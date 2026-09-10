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
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es requerido.")
            .EmailAddress().WithMessage("El formato del correo no es válido.")
            .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("El nombre del negocio es requerido.")
            .MaximumLength(150).WithMessage("El nombre del negocio no puede exceder 150 caracteres.");
    }
}
