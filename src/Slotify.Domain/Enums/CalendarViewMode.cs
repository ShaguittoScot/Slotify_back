namespace Slotify.Domain.Enums;

/// <summary>
/// Modos de visualización del calendario táctil.
/// No tiene tabla en BD — es un concepto de presentación del frontend.
/// Relacionado con: US-003 (Navegación Multiformato en Calendario Táctil)
/// </summary>
public enum CalendarViewMode
{
    /// <summary>Vista de un solo día con franjas horarias.</summary>
    Day = 0,

    /// <summary>Vista semanal con columnas por día.</summary>
    Week = 1,

    /// <summary>Vista mensual con cuadrícula.</summary>
    Month = 2
}
