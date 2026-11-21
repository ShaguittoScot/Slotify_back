---
title: US-003 - Calendario Tactil Multiformato y Consulta de Citas
aliases:
  - US-003
  - Calendario y Citas
tags:
  - historia-usuario
  - calendario
  - agenda
  - citas
  - us-003
estado_documental: "Original verificada"
implementacion_backend: "Completa"
integracion_frontend: "Parcial"
---

# 📅 US-003: Calendario Táctil Multiformato y Consulta de Citas

* **Como:** Administrador o empleado del negocio.
* **Quiero:** Visualizar la agenda táctil interactiva en modos de Día, Semana y Mes con filtros y consulta unificada de citas y bloqueos.
* **Para:** Conocer los compromisos operativos, consultar detalles de clientes, prevenir empalmes de turnos y coordinar la jornada diaria.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Consulta unificada de slots por rango de fechas
```gherkin
Dado que el usuario navega a la Agenda autenticado con su token JWT de Slotify
Cuando selecciona una fecha o cambia entre vistas (Día, Semana o Mes)
Entonces el frontend calcula el rango ISO 8601 [startDate, endDate]
Y envía GET /api/calendar/slots?startDate=...&endDate=...
Y el backend resuelve el BusinessId desde el claim del JWT
Y consulta citas, bloqueos_agenda y horarios_disponibilidad
Y responde con CalendarResponse ({ rangeStart, rangeEnd, slots }) unificando citas y bloqueos con sus respectivos identificadores, estados y colores.
```

### Escenario 2: Visualización de estado vacío (Empty State)
```gherkin
Dado que el negocio no tiene citas ni bloqueos registrados en el período consultado
Cuando el backend responde con slots: []
Entonces el frontend renderiza la grilla del calendario limpia
Y no genera citas simuladas para usuarios reales en producción
Y permite interactuar directamente con la grilla para agendar o bloquear horarios.
```

---

## ⚙️ Implementación Backend Real

* **Controlador:** `CalendarController.cs`
* **Ruta HTTP:** `GET /api/calendar/slots`
* **Consulta MediatR:** `GetCalendarSlotsQuery` / `GetCalendarSlotsHandler`
* **DTOs de Respuesta:** `CalendarResponse`, `CalendarSlotDto` (formato plano con campos opcionales según `type: "appointment" | "block" | "available_range"`)
* **Entidades Involucradas:** [[Entidad Appointment]], [[Entidad ScheduleBlock]], [[Entidad AvailabilitySchedule]], [[Entidad Business]]
* **Tablas de Base de Datos Impactadas:** `citas`, `bloqueos_agenda`, `horarios_disponibilidad`, `negocios`

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `CalendarView.tsx`, `DayView.tsx`, `WeekView.tsx`, `MonthView.tsx`, `useCalendarStore`, `calendarApi`
* **Nivel de Integración:** Parcial.
* **Detalle de Integración:**
  - La consulta `GET /api/calendar/slots` está totalmente integrada en el cliente a través de `useCalendarStore.fetchSlots()` y `calendarApi.getSlots()`.
  - La creación y confirmación de citas en backend se delega a la acción `POST /api/calendar/appointments`, documentada de forma individual en [[US-011 - Agendamiento y Creacion de Citas]]. Dicho endpoint no cuenta actualmente con un consumidor activo en la UI del frontend, que aún gestiona citas en memoria local.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas de Repositorio:** `AppointmentRepositoryTests.cs`, `AvailabilityScheduleRepositoryTests.cs`.
* **Pruebas de Integración:** `ProtectedEndpointsTests.cs` (aislamiento por negocio y validación de parámetros de fecha).

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-011 - Agendamiento y Creacion de Citas]]
* [[US-008 - Bloqueo Manual de Horarios y Disponibilidad]]
* [[Feature Calendar]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad Appointment]]
* [[Entidad ScheduleBlock]]
* [[Entidad AvailabilitySchedule]]
* [[Flujo de Consulta de Citas y Estado Vacio]]
