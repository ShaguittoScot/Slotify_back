namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio para plantillas de sector (giros comerciales).
/// Relacionado con: US-006 (Selector de Giro Comercial),
///                  US-002 (Implementación de Plantillas)
/// 
/// NOTA: SectorTemplate usa int PK, por lo que no hereda de IRepository&lt;T&gt;
/// (que requiere BaseEntity con Guid PK).
/// </summary>
public interface ISectorTemplateRepository
{
    /// <summary>
    /// Obtiene todas las plantillas de sector disponibles.
    /// Usado en US-006 para mostrar el selector de giro comercial.
    /// </summary>
    Task<IReadOnlyList<SectorTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una plantilla por su ID.
    /// Usado en US-002 al aplicar la plantilla al negocio.
    /// </summary>
    Task<SectorTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
