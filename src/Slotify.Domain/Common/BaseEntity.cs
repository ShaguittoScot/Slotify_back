namespace Slotify.Domain.Common;

/// <summary>
/// Base entity with common properties for all domain entities.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
