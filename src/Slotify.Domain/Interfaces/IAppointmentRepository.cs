namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para citas/reservas.
/// Relacionado con: US-003 (Calendario muestra citas existentes)
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
    /// Obtiene las citas de un empleado específico en un rango de fechas.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByEmployeeAndDateRangeAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
