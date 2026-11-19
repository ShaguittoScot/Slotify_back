namespace Slotify.Domain.Interfaces;

using Slotify.Domain.Entities;

/// <summary>
/// Repositorio de solo lectura para plantillas de sector / giros comerciales.
/// Hereda de IReadRepository&lt;SectorTemplate, int&gt; cumpliendo ISP (Interface Segregation Principle).
/// </summary>
public interface ISectorTemplateRepository : IReadRepository<SectorTemplate, int>
{
}
