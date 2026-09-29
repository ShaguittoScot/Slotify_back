---
title: Entidad ScheduleBlock
tags:
  - domain
  - entity
  - blocks
---

# 🚫 Entidad `ScheduleBlock`

Ubicación: `src/Slotify.Domain/Entities/ScheduleBlock.cs`  
Tabla en PostgreSQL: `"ScheduleBlocks"`

Representa una franja horaria bloqueada manualmente para impedir la creación de citas.

---

## 📋 Propiedades Principales
* **`Id` (`long`):** Clave primaria autoincremental.
* **`BusinessId` (`Guid`):** FK al [[Entidad Business]].
* **`EmployeeId` (`Guid?`):** FK opcional a [[Entidad User]] si el bloqueo es para un empleado específico.
* **`StartDate` (`DateTime`):** Inicio del bloqueo (UTC).
* **`EndDate` (`DateTime`):** Fin del bloqueo (UTC).
* **`Reason` (`string?`):** Motivo del bloqueo (ej. "Mantenimiento", "Vacaciones").

---

## 🔗 Relaciones en el Grafo
- **Pertenece a:** [[Modulo Domain]]
- **Relacionado con:** [[Entidad Business]], [[Entidad User]]
- **Usado en:** [[Feature ScheduleBlocks]], [[Flujo de Bloqueo de Horarios]], [[Feature Calendar]]
