namespace Slotify.Domain.Common;

/// <summary>
/// Auditable entity with tracking fields for creation and modification.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? CreatedBy { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? UpdatedBy { get; set; }
}
