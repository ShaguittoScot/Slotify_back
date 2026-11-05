---
title: US-003 - Calendario Tactil Multiformato y Consulta de Citas
tags:
  - historia-usuario
  - calendario
  - agenda
  - citas
---

# 📅 US-003: Calendario Táctil Multiformato y Consulta de Citas

* **Como:** Administrador o empleado del negocio.
* **Quiero:** Visualizar la agenda táctil interactiva en modos de Día, Semana y Mes, con filtros rápidos por estado (Pendiente, Confirmada, Cobrada, Bloqueo).
* **Para:** Conocer los compromisos del día, consultar detalles de clientes, cobrar citas y reprogramar turnos rápidamente.

---

## 🎯 Criterios de Aceptación (Gherkin)

`gherkin
Escenario: Consulta de slots por rango de fechas
  Dado que el usuario navega a la Agenda
  Cuando selecciona una fecha o cambia entre vistas (Día/Semana/Mes)
  Entonces el frontend calcula el rango [startDate, endDate]
  Y envía GET /api/calendar/slots?startDate=...&endDate=... con el JWT
  Y el backend responde con la lista unificada de citas y bloqueos
  Y la interfaz renderiza los slots con codificación de color por estado.

Escenario: Manejo de cuenta Demo vs Cuenta Real
  Dado que un usuario real no tiene citas registradas en el período
  Cuando el backend devuelve slots: []
  Entonces el frontend muestra la grilla vacía permitiendo agendamiento táctil (Tap-to-Book)
  Y no genera citas ficticias.
`

---

## 🔗 Componentes Asociados
* **Backend:** [[CalendarController]], GetCalendarSlotsQueryHandler.cs, [[Entidad Appointment]], [[Entidad ScheduleBlock]]
* **Frontend:** CalendarView.tsx, DayView.tsx, WeekView.tsx, MonthView.tsx, useCalendarStore, calendarApi
* **Flujos:** [[Flujo de Consulta de Citas y Estado Vacio]]
