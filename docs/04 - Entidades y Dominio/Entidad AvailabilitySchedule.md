---
title: Entidad AvailabilitySchedule
tags:
  - domain
  - entity
  - schedules
---

# ⏰ Entidad `AvailabilitySchedule`

Ubicación: `src/Slotify.Domain/Entities/AvailabilitySchedule.cs`  
Tabla en PostgreSQL: `"AvailabilitySchedules"`

Define los horarios regulares de operación semanal de un negocio o turnos de trabajo de un empleado (por ejemplo: Lunes a Viernes de 09:00 a 18:00).

---

## 📋 Propiedades Principales
* **`Id` (`long`):** Clave primaria.
* **`BusinessId` (`Guid`):** FK al [[Entidad Business]].
* **`EmployeeId` (`Guid?`):** FK opcional a [[Entidad User]].
* **`DayOfWeek` (`int`):** Día de la semana (0 = Domingo, 6 = Sábado).
* **`StartTime` (`TimeSpan`):** Hora de apertura.
* **`EndTime` (`TimeSpan`):** Hora de cierre.
* **`IsActive` (`bool`):** Si aplica o está inactivo.

---

## 🔗 Relaciones en el Grafo
- **Pertenece a:** [[Modulo Domain]]
- **Relacionado con:** [[Entidad Business]], [[Entidad User]]
- **Usado en:** [[Feature Calendar]], [[Flujo de Disponibilidad de Calendario]]
