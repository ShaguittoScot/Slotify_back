---
title: US-008 - Bloqueo Manual de Horarios y Disponibilidad
tags:
  - historia-usuario
  - bloqueos
  - agenda
  - disponibilidad
---

# ⛔ US-008: Bloqueo Manual de Horarios y Disponibilidad

* **Como:** Dueño de negocio o empleado.
* **Quiero:** Crear bloqueos de tiempo en la agenda para horarios de comida, descansos o mantenimiento técnico.
* **Para:** Prevenir que los clientes reserven turnos en intervalos no laborables.

---

## 🎯 Criterios de Aceptación (Gherkin)

`gherkin
Escenario: Creación de un bloqueo horario
  Dado que el usuario abre el modal de bloqueo desde la agenda
  Cuando especifica fecha, hora inicio, hora fin, motivo y profesional asignado
  Entonces el sistema envía POST /api/schedule-blocks
  Y el backend persiste la entidad en bloqueos_agenda
  Y la agenda táctil renderiza el slot bloqueado con indicador visual y deshabilita reservas.
`

---

## 🔗 Componentes Asociados
* **Backend:** ScheduleBlocksController.cs, CreateScheduleBlockCommandHandler.cs, [[Entidad ScheduleBlock]], IScheduleBlockRepository
* **Frontend:** BlockSlotModal.tsx, eatures/schedule-blocks/api/index.ts, useCalendarStore
* **Flujos:** [[Flujo de Bloqueo de Horarios]]
