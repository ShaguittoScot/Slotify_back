namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para bloqueos manuales de agenda.
/// Hereda de IRepository&lt;ScheduleBlock, long&gt; cumpliendo ISP, LSP y consistencia arquitectónica.
/// 
/// Relacionado con: US-008 (Bloqueo Manual de Horarios), US-022 (Aislamiento por negocio) y US-023.
/// </summary>
public interface IScheduleBlockRepository : IRepository<ScheduleBlock, long>
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
    /// Obtiene los bloqueos asociados a un colaborador específico (o globales del negocio) en un rango de fechas.
    /// </summary>
    Task<IReadOnlyList<ScheduleBlock>> GetByEmployeeAndDateRangeAsync(
        Guid businessId,
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si existe algún solapamiento o conflicto de horario con un bloqueo existente.
    /// </summary>
    Task<bool> HasConflictAsync(
        Guid businessId,
        Guid? employeeId,
        DateTime startDateTime,
        DateTime endDateTime,
        long? excludeBlockId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Alias de compatibilidad hacia AddAsync.
    /// </summary>
    Task<ScheduleBlock> CreateAsync(ScheduleBlock block, CancellationToken cancellationToken = default) =>
        AddAsync(block, cancellationToken);
}
