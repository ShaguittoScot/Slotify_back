namespace Slotify.Domain.Enums;

/// <summary>
/// Roles de usuario en el sistema.
/// Mapea a: CHECK (rol IN ('DUENO', 'EMPLEADO'))
/// Relacionado con: US-000 (Registro — se registra como Owner/DUENO)
/// </summary>
public enum UserRole
{
    /// <summary>Dueño del negocio (DUENO). Tiene control total.</summary>
    Owner = 0,

    /// <summary>Empleado del negocio (EMPLEADO). Acceso limitado.</summary>
    Employee = 1
}
