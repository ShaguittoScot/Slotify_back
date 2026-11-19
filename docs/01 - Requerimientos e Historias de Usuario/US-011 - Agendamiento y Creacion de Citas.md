---
title: US-011 - Agendamiento y Creacion de Citas
aliases:
  - US-011
  - Agendamiento de Citas
tags:
  - historia-usuario
  - citas
  - agenda
  - calendar
  - us-011
estado_documental: "Reconstruida desde implementación"
implementacion_backend: "Completa"
integracion_frontend: "No integrada"
---

# 📝 US-011: Agendamiento y Creación de Citas

> [!warning] Reconstruida a partir del código. Pendiente de validación funcional.
> Esta historia de usuario no contaba con una nota original independiente en los documentos de requerimientos iniciales. Se formaliza a partir de la implementación real de `CreateAppointmentCommand`, `CalendarController.cs` y su referencia documental cruzada con `US-003`.

* **Como:** Empleado o administrador del negocio.
* **Quiero:** Registrar una nueva cita indicando cliente, horario, servicio, notas y precio en la agenda del negocio.
* **Para:** Agendar compromisos de forma inmediata, evitar traslapes de horarios y confirmar la reserva de servicios.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Creación exitosa de cita
```gherkin
Dado que el usuario autenticado cuenta con un token JWT válido con claim businessId
Cuando envía una solicitud POST /api/calendar/appointments con los datos:
  | clientName | clientPhone | clientEmail | serviceName | startDateTime | endDateTime | price |
Entonces el backend valida que endDateTime sea estrictamente posterior a startDateTime
Y comprueba que no existan traslapes horarios con bloqueos activos o citas confirmadas
Y persiste la cita en la tabla citas con estado PENDING o CONFIRMED asociada al id_negocio
Y responde con código HTTP 201 Created y el identificador de la cita creada.
```

### Escenario 2: Rechazo por traslape o rango horario inválido
```gherkin
Dado que la fecha/hora de finalización es anterior o igual a la de inicio
O que existe un bloqueo en la tabla bloqueos_agenda en la misma franja
Cuando se envía la solicitud de creación de cita
Entonces el validador CreateAppointmentValidator o el handler rechazan la operación
Y retornan código HTTP 400 Bad Request detallando el conflicto.
```

---

## ⚙️ Implementación Backend Real

* **Controlador:** `CalendarController.cs`
* **Ruta HTTP:** `POST /api/calendar/appointments`
* **Comando MediatR:** `CreateAppointmentCommand` / `CreateAppointmentHandler`
* **Validador:** `CreateAppointmentValidator`
* **Entidades Involucradas:** [[Entidad Appointment]], [[Entidad Business]]
* **Tablas de Base de Datos Impactadas:** `citas`, `negocios`

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `CalendarView.tsx`, `CreateAppointmentModal` (o flujo local)
* **Nivel de Integración:** No integrada.
* **Detalle:**
  - Actualmente el frontend visualiza slots mediante `GET /api/calendar/slots` ([[US-003 - Calendario Tactil Multiformato y Consulta de Citas]]), pero no tiene conectada la invocación HTTP a `POST /api/calendar/appointments`.
  - La creación de citas se realiza de forma simulada en memoria en `useCalendarStore`, requiriendo conectar la mutación hacia el endpoint real del backend.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas de Repositorio:** `AppointmentRepositoryTests.cs`.
* **Pruebas Unitarias:** `CreateAppointmentValidatorTests.cs`.
* **Pruebas de Integración:** `ProtectedEndpointsTests.cs`.

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-003 - Calendario Tactil Multiformato y Consulta de Citas]]
* [[Feature Calendar]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad Appointment]]
* [[Entidad Business]]
* [[Flujo General de Peticion]]
