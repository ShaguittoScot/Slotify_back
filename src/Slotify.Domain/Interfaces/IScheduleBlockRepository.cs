namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para bloqueos manuales de agenda.
/// Relacionado con: US-008 (Bloqueo Manual de Horarios No Disponibles)
/// 
/// NOTA: ScheduleBlock usa long PK — no hereda de IRepository&lt;T&gt;.
/// </summary>
public interface IScheduleBlockRepository
{
    /// <summary>
    /// Obtiene los bloqueos de un negocio en un rango de fechas.
    /// Usado en US-003/US-008 para mostrar bloqueos en el calendario.
    /// </summary>
    Task<IReadOnlyList<ScheduleBlock>> GetByDateRangeAsync(
        Guid businessId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un nuevo bloqueo de agenda.
    /// Acción principal de US-008.
    /// </summary>
    Task<ScheduleBlock> CreateAsync(ScheduleBlock block, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un bloqueo por su ID.
    /// </summary>
    Task<ScheduleBlock?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un bloqueo existente (desbloquear).
    /// Acción complementaria de US-008.
    /// </summary>
    Task DeleteAsync(ScheduleBlock block, CancellationToken cancellationToken = default);
}
