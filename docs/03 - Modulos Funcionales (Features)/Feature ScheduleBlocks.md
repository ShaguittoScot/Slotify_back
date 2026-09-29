---
title: Feature ScheduleBlocks (Bloqueos de Agenda)
tags:
  - feature
  - schedule
  - blocks
---

# 📅 Feature ScheduleBlocks

Ubicación en el código: `src/Slotify.Application/Features/ScheduleBlocks/`

Permite crear, listar y eliminar bloqueos manuales de tiempo en la agenda de un negocio o empleado.

---

## 📂 Estructura de Archivos
* **Comandos:**
  * `CreateScheduleBlockCommand.cs`: Datos para bloquear (`BusinessId`, `EmployeeId`, `StartDate`, `EndDate`, `Reason`).
  * `DeleteScheduleBlockCommand.cs`: Datos para eliminar (`BlockId`, `BusinessId`).
* **Queries:**
  * `GetScheduleBlocksQuery.cs`: Parámetros de búsqueda (`BusinessId`, `StartDate`, `EndDate`, `EmployeeId`).
* **Handlers:**
  * `CreateScheduleBlockHandler.cs`
  * `DeleteScheduleBlockHandler.cs`
  * `GetScheduleBlocksHandler.cs`
* **Validadores:**
  * `CreateScheduleBlockValidator.cs`
* **DTOs:**
  * `ScheduleBlockDto.cs`

---

## 🔗 Relaciones en el Grafo
- **Invocado por:** [[Modulo API]] (`ScheduleBlocksController`)
- **Manipula:** [[Entidad ScheduleBlock]], [[Entidad Business]]
- **Flujo:** [[Flujo de Bloqueo de Horarios]]
- **Repositorio:** [[Repositorios]] (`ScheduleBlockRepository`)
