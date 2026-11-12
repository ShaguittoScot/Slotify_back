namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para citas/reservas.
/// Relacionado con: US-003 (Calendario muestra citas existentes), US-022 (Aislamiento por negocio) y US-023.
/// </summary>
public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>
    /// Obtiene las citas de un negocio en un rango de fechas.
    /// Usado en US-003 para poblar el calendario.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByDateRangeAsync(
        Guid businessId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene las citas de un empleado específico dentro de un negocio en un rango de fechas.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByEmployeeAndDateRangeAsync(
        Guid businessId,
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida si existe alguna cita confirmada activa que se solape con el intervalo indicado.
    /// </summary>
    Task<bool> HasActiveAppointmentConflictAsync(
        Guid businessId,
        Guid? employeeId,
        DateTime startDate,
        DateTime endDate,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);
}
