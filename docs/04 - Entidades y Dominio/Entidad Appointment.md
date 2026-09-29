---
title: Entidad Appointment
tags:
  - domain
  - entity
  - appointments
---

# 🎟️ Entidad `Appointment`

Ubicación: `src/Slotify.Domain/Entities/Appointment.cs`  
Tabla en PostgreSQL: `"Appointments"`

Representa una cita o turno agendado por un cliente con un empleado o negocio.

---

## 📋 Propiedades Principales
* **`Id` (`Guid`):** Clave primaria.
* **`BusinessId` (`Guid`):** FK al [[Entidad Business]].
* **`ClientId` (`Guid`):** FK a [[Entidad User]] (Cliente que reserva).
* **`EmployeeId` (`Guid?`):** FK a [[Entidad User]] (Empleado que atiende).
* **`StartTime` / `EndTime` (`DateTime`):** Horario pactado en UTC.
* **`Status` (`string`):** Estado de la cita (`PENDIENTE`, `CONFIRMADA`, `CANCELADA`, `COMPLETADA`).

---

## 🔗 Relaciones en el Grafo
- **Pertenece a:** [[Modulo Domain]]
- **Relacionado con:** [[Entidad Business]], [[Entidad User]], [[Entidad ScheduleBlock]]
- **Usado en:** [[Feature Calendar]]
