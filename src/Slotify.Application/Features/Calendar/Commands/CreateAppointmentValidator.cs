using FluentValidation;

namespace Slotify.Application.Features.Calendar.Commands;

/// <summary>
/// Reglas de validación para el comando CreateAppointmentCommand mediante FluentValidation.
/// Se ejecuta automáticamente en el PipelineBehavior de MediatR antes de llegar al Handler.
/// </summary>
public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("El nombre del cliente es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del cliente no puede exceder 150 caracteres.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("La fecha y hora de inicio es requerida.")
            .LessThan(x => x.EndTime).WithMessage("La hora de inicio debe ser anterior a la hora de fin.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("La fecha y hora de fin es requerida.")
            .GreaterThan(x => x.StartTime).WithMessage("La hora de fin debe ser posterior a la hora de inicio.");

        RuleFor(x => x.AgreedTotal)
            .GreaterThanOrEqualTo(0).WithMessage("El total pactado debe ser mayor o igual a cero.");

        When(x => !string.IsNullOrWhiteSpace(x.ClientEmail), () =>
        {
            RuleFor(x => x.ClientEmail)
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ClientPhone), () =>
        {
            RuleFor(x => x.ClientPhone)
                .MaximumLength(30).WithMessage("El teléfono no puede exceder 30 caracteres.");
        });
    }
}
