namespace Slotify.Application.Features.Onboarding.DTOs;

/// <summary>
/// Representa el horario de apertura del negocio para un dÃ­a.
/// No corresponde a la disponibilidad de un empleado.
/// </summary>
public record OnboardingBusinessHourDto
{
    /// <summary>
    /// DÃ­a de la semana: 0=Domingo, 1=Lunes, ..., 6=SÃ¡bado.
    /// </summary>
    public required short DayOfWeek { get; init; }

    /// <summary>
    /// Hora de apertura en formato HH:mm.
    /// </summary>
    public required TimeOnly StartTime { get; init; }

    /// <summary>
    /// Hora de cierre en formato HH:mm.
    /// </summary>
    public required TimeOnly EndTime { get; init; }
}
