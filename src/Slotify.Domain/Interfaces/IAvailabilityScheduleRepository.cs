namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para horarios de disponibilidad recurrentes.
/// Relacionado con: US-003 (Calendario — determina slots disponibles por día)
/// 
/// NOTA: AvailabilitySchedule usa long PK — no hereda de IRepository&lt;T&gt;.
/// </summary>
public interface IAvailabilityScheduleRepository
{
    /// <summary>
    /// Obtiene los horarios de un negocio para un día de la semana.
    /// Usado en US-003 para construir la vista diaria del calendario.
    /// </summary>
    /// <param name="businessId">ID del negocio.</param>
    /// <param name="dayOfWeek">Día de la semana (0=Dom..6=Sáb).</param>
    Task<IReadOnlyList<AvailabilitySchedule>> GetByBusinessAndDayAsync(
        Guid businessId,
        short dayOfWeek,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todos los horarios de un negocio (todos los días).
    /// Usado en US-003 para la vista semanal/mensual.
    /// </summary>
    Task<IReadOnlyList<AvailabilitySchedule>> GetByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);
}
