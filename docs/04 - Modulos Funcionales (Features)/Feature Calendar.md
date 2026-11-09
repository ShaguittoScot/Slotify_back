---
title: Feature Calendar (Disponibilidad y Turnos)
tags:
  - feature
  - calendar
  - slots
---

# 🗓️ Feature Calendar

Ubicación en el código: `src/Slotify.Application/Features/Calendar/`

Calcula los slots o franjas horarias disponibles para agendar citas, tomando en cuenta el horario base del negocio ([[Entidad AvailabilitySchedule]]), los bloqueos activos ([[Entidad ScheduleBlock]]) y las citas ya reservadas ([[Entidad Appointment]]).

---

## 📂 Estructura de Archivos
* **Queries:**
  * `GetCalendarSlotsQuery.cs`: Parámetros (`BusinessId`, `EmployeeId`, `Date`).
* **Handlers:**
  * `GetCalendarSlotsHandler.cs`: Algoritmo de cálculo de disponibilidad de franjas.

---

## 🔗 Relaciones en el Grafo
- **Invocado por:** [[Modulo API]] (`CalendarController`)
- **Consulta:** [[Entidad AvailabilitySchedule]], [[Entidad ScheduleBlock]], [[Entidad Appointment]]
- **Flujo:** [[Flujo de Disponibilidad de Calendario]]
