using FluentValidation;

namespace Slotify.Application.Features.ScheduleBlocks.Commands;

/// <summary>
/// Validaciones para la creación de bloqueos de agenda.
/// Relacionado con: US-008
/// Mapea a: CONSTRAINT check_bloqueo_valido CHECK (fecha_hora_inicio &lt; fecha_hora_fin)
/// </summary>
public class CreateScheduleBlockValidator : AbstractValidator<CreateScheduleBlockCommand>
{
    public CreateScheduleBlockValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("El ID del negocio es requerido.");

        RuleFor(x => x.StartDateTime)
            .NotEmpty().WithMessage("La fecha/hora de inicio es requerida.")
            .LessThan(x => x.EndDateTime)
                .WithMessage("La hora de inicio debe ser anterior a la hora de fin.");

        RuleFor(x => x.EndDateTime)
            .NotEmpty().WithMessage("La fecha/hora de fin es requerida.")
            .GreaterThan(x => x.StartDateTime)
                .WithMessage("La hora de fin debe ser posterior a la hora de inicio.");

        RuleFor(x => x.Reason)
            .MaximumLength(255)
                .WithMessage("El motivo no puede exceder 255 caracteres.");
    }
}
