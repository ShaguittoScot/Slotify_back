---
title: Bloqueos de Agenda (Especificación de Endpoints)
aliases:
  - Bloqueos de Agenda
  - Endpoints Bloqueos
tags:
  - api
  - endpoints
  - turnos
  - schedule-blocks
---

# 📅 API: Bloqueos de Agenda (`ScheduleBlocks`)

> [!abstract] Referencia de Contratos HTTP
> Esta nota detalla la especificación técnica de los endpoints expuestos por `ScheduleBlocksController`. Para la documentación conceptual del módulo, casos de uso y validaciones completas, consultar [[Feature ScheduleBlocks]].

* **Controlador:** `ScheduleBlocksController`
* **Ruta base:** `/api/schedule-blocks`
* **Autenticación:** Requerida (`[Authorize]` con JWT). El `BusinessId` es extraído automáticamente del token.

---

## 1. Obtener Bloqueos de Agenda

Consulta los bloqueos existentes para el negocio autenticado dentro de una ventana temporal.

* **Método:** `GET`
* **URL:** `/api/schedule-blocks`
* **Query Parameters:**
  - `startDate` (`DateTime` ISO 8601 UTC, Requerido): Ej. `2026-11-01T00:00:00Z`
  - `endDate` (`DateTime` ISO 8601 UTC, Requerido): Ej. `2026-11-30T23:59:59Z`
  - `employeeId` (`Guid`, Opcional): Filtrar bloqueos específicos de un empleado.

### Ejemplo de Petición HTTP
```http
GET /api/schedule-blocks?startDate=2026-11-01T00:00:00Z&endDate=2026-11-30T23:59:59Z HTTP/1.1
Host: localhost:5272
Authorization: Bearer <JWT_SLOTIFY>
```

### Respuesta Exitosa (`200 OK`)
```json
{
  "data": [
    {
      "id": 14,
      "businessId": "b8f2d1e0-3c4a-4a5e-b9f1-7c8d9e0f1a2b",
      "employeeId": null,
      "startDateTime": "2026-11-21T14:00:00Z",
      "endDateTime": "2026-11-21T16:00:00Z",
      "reason": "Mantenimiento local"
    }
  ],
  "success": true,
  "message": null,
  "errors": []
}
```

---

## 2. Crear un Bloqueo Manual

Inhabilita una franja horaria en la agenda.

* **Método:** `POST`
* **URL:** `/api/schedule-blocks`
* **Headers:** `Content-Type: application/json`, `Authorization: Bearer <TOKEN>`

### Payload (`CreateScheduleBlockCommand`)
```json
{
  "employeeId": null,
  "startDateTime": "2026-11-21T14:00:00Z",
  "endDateTime": "2026-11-21T16:00:00Z",
  "reason": "Mantenimiento local"
}
```

### Respuesta Exitosa (`201 Created`)
```json
{
  "data": 15,
  "success": true,
  "message": null,
  "errors": []
}
```

---

## 3. Eliminar un Bloqueo (Desbloquear)

Libera la franja temporal validando tenencia.

* **Método:** `DELETE`
* **URL:** `/api/schedule-blocks/{id}`
* **Parámetro de Ruta:** `id` (`long`, identificador numérico `BIGSERIAL`).

### Respuesta Exitosa (`200 OK`)
```json
{
  "success": true,
  "message": null,
  "errors": []
}
```

### Respuestas de Error
* `401 Unauthorized`: Token faltante o inválido.
* `403 Forbidden`: El bloqueo especificado pertenece a otro negocio.
* `404 NotFound`: El bloqueo no existe.

---

## 🔗 Trazabilidad
* **Módulo Principal:** [[Feature ScheduleBlocks]]
* **Entidad de Dominio:** [[Entidad ScheduleBlock]]
* **Controlador:** [[Modulo API]]
* **Historias de Usuario:** [[US-008 - Bloqueo Manual de Horarios y Disponibilidad]]
* **Flujos:** [[Flujo de Bloqueo de Horarios]]
