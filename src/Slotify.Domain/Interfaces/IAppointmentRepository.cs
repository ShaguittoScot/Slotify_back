namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para citas/reservas.
/// Relacionado con: US-003 (Calendario muestra citas existentes), US-022 (Aislamiento por negocio).
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
    /// Obtiene el historial completo de citas de un negocio ordenadas de la más reciente a la más antigua.
    /// Usado en US-026 (Construcción de módulo de clientes) con aislamiento estricto por BusinessId.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);
}
