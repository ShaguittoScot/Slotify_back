namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para citas/reservas.
/// Relacionado con: US-003 (Calendario muestra citas existentes), US-022 (Aislamiento por negocio) y US-023.
/// </summary>
public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>
    /// Obtiene todas las citas de un negocio en un rango de fechas.
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

    /// <summary>
    /// Obtiene las citas asociadas a un cliente consumidor por su ID de cliente o correo electrónico.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByConsumerIdentityAsync(
        Guid clientId,
        string? email,
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

    /// <summary>
    /// Obtiene las citas de un cliente específico (por ClientId) en un rango de fechas.
    /// US-Notifications: Permite al cliente ver sus propias citas próximas.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByClientIdAsync(
        Guid clientId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
