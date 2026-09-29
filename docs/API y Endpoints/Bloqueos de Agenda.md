---
title: Bloqueos de Agenda
tags:
  - api
  - endpoints
  - turnos
---

# 📅 API: Bloqueos de Agenda (`ScheduleBlocks`)

Controlador: `ScheduleBlocksController`  
Ruta base: `/api/schedule-blocks`

Permite a los negocios bloquear franjas horarias para evitar que se reserven citas durante periodos de indisponibilidad.

---

## 1. Obtener Bloqueos

* **Método:** `GET`
* **URL:** `/api/schedule-blocks`
* **Query Params:**
  * `startDate` (DateTime ISO UTC, Requerido): `2026-01-01T00:00:00Z`
  * `endDate` (DateTime ISO UTC, Requerido): `2026-12-31T23:59:59Z`
  * `businessId` (Guid, Opcional)
  * `employeeId` (Guid, Opcional)

### Ejemplo de Petición:
```http
GET /api/schedule-blocks?startDate=2026-01-01T00:00:00Z&endDate=2026-12-31T23:59:59Z HTTP/1.1
Host: localhost:5272
```

### Respuesta Exitosa (`200 OK`):
```json
{
  "data": [
    {
      "id": 1,
      "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "employeeId": null,
      "startDate": "2026-09-25T14:00:00Z",
      "endDate": "2026-09-25T16:00:00Z",
      "reason": "Mantenimiento local"
    }
  ],
  "success": true,
  "message": null,
  "errors": []
}
```

---

## 2. Crear un Bloqueo

* **Método:** `POST`
* **URL:** `/api/schedule-blocks`

### Body:
```json
{
  "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "employeeId": null,
  "startDate": "2026-09-25T14:00:00Z",
  "endDate": "2026-09-25T16:00:00Z",
  "reason": "Mantenimiento local"
}
```

---

## 3. Eliminar un Bloqueo (Desbloquear)

* **Método:** `DELETE`
* **URL:** `/api/schedule-blocks/{id}`

---

> [[Dashboard|⬅️ Volver al Dashboard]]
