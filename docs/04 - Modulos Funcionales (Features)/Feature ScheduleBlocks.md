---
title: Feature ScheduleBlocks (Bloqueos de Agenda)
aliases:
  - Feature ScheduleBlocks
  - Bloqueos de Agenda
  - ScheduleBlocks Feature
tags:
  - feature
  - schedule
  - blocks
  - agenda
  - turnos
---

# 📅 Feature ScheduleBlocks (Gestión y Bloqueo de Horarios)

> [!abstract] Propósito del Módulo
> Permite a los administradores y profesionales de un negocio inhabilitar franjas temporales en la agenda (comida, vacaciones, reuniones de equipo, mantenimiento de instalaciones) impidiendo que los clientes reserven turnos en dichos lapsos.

---

## 🧭 Estado de Implementación

| Operación / Endpoint | Implementado | Probado | Integrado en Frontend | Estado Global |
| :--- | :---: | :---: | :---: | :---: |
| `POST /api/schedule-blocks` (Crear Bloqueo) | ✅ Sí | ✅ Sí (`ProtectedEndpointsTests`) | ✅ Sí (`BlockSlotModal.tsx`) | **Implementado, Probado e Integrado** |
| `GET /api/schedule-blocks` (Listar en Rango) | ✅ Sí | ✅ Sí (`ProtectedEndpointsTests`) | ✅ Sí (`scheduleBlocksApi`) | **Implementado, Probado e Integrado** |
| `DELETE /api/schedule-blocks/{id}` (Desbloquear) | ✅ Sí | ✅ Sí (`ProtectedEndpointsTests`) | ✅ Sí (`BlockSlotModal.tsx`) | **Implementado, Probado e Integrado** |

---

## 🛠️ Controlador y Rutas Asociadas

* **Controlador:** `src/Slotify.API/Controllers/ScheduleBlocksController.cs`
* **Ruta base:** `/api/schedule-blocks`
* **Seguridad:** Requiere autenticación `[Authorize]`. El `BusinessId` se extrae del JWT vía `ICurrentUserService`.

| Método | Ruta | Parámetros | Descripción | Códigos HTTP |
| :--- | :--- | :--- | :--- | :---: |
| `POST` | `/api/schedule-blocks` | Body: `CreateScheduleBlockCommand` | Crea un nuevo bloqueo manual en el negocio autenticado. | `201`, `400`, `401` |
| `GET` | `/api/schedule-blocks` | Query: `startDate`, `endDate`, `employeeId?` | Consulta los bloqueos registrados en el intervalo. | `200`, `401`, `403` |
| `DELETE` | `/api/schedule-blocks/{id}` | Ruta: `id` (`long`) | Elimina un bloqueo existente (desbloqueo de horario). | `200`, `401`, `403`, `404` |

---

## ⚙️ Commands, Queries, Handlers y Validaciones

```mermaid
graph TD
    Client["📱 Frontend"] --> BlkCtrl["ScheduleBlocksController (/api/schedule-blocks)"]

    BlkCtrl -->|POST /| CmdCreate["CreateScheduleBlockCommand"]
    BlkCtrl -->|GET /| QryGet["GetScheduleBlocksQuery"]
    BlkCtrl -->|DELETE /{id}| CmdDel["DeleteScheduleBlockCommand"]

    CmdCreate --> ValCreate["🛡️ CreateScheduleBlockValidator"]

    CmdCreate --> HdlCreate["CreateScheduleBlockHandler"]
    QryGet --> HdlGet["GetScheduleBlocksHandler"]
    CmdDel --> HdlDel["DeleteScheduleBlockHandler"]

    HdlCreate --> Repo["IScheduleBlockRepository"]
    HdlDel --> Repo
    HdlGet --> Repo
```

### 1. `CreateScheduleBlockCommand` (US-008)
* **Handler:** `CreateScheduleBlockHandler.cs`
* **Validador:** `CreateScheduleBlockValidator.cs` (`FluentValidation`):
  - `StartDateTime`: Obligatorio y estrictamente menor que `EndDateTime`.
  - `EndDateTime`: Obligatorio y mayor que `StartDateTime`.
  - `Reason`: Opcional, máximo 255 caracteres.
* **Lógica de Persistencia:**
  1. Extrae `BusinessId` de `ICurrentUserService`.
  2. Crea la entidad [[Entidad ScheduleBlock]] persistiendo en la tabla `"bloqueos_agenda"`.
  3. Retorna `Result<long>` con el ID autoincremental `BIGSERIAL` creado (HTTP 201 Created).

### 2. `GetScheduleBlocksQuery`
* **Handler:** `GetScheduleBlocksHandler.cs`
* **Lógica:** Recupera mediante `IScheduleBlockRepository.GetByDateRangeAsync()` todos los bloqueos del negocio entre `startDate` y `endDate`. Si se envió `employeeId`, filtra los bloqueos de dicho empleado o generales del negocio.

### 3. `DeleteScheduleBlockCommand` (US-023)
* **Handler:** `DeleteScheduleBlockHandler.cs`
* **Validación de Tenancy (US-022):**
  1. Obtiene el bloqueo por su `BlockId`.
  2. Valida que el `block.BusinessId` coincida con el `businessId` del usuario autenticado.
  3. Si no coincide, rechaza con `ErrorType.Forbidden` (HTTP 403).
  4. Si coincide, elimina el registro y confirma la transacción.

---

## 📦 Modelos y DTOs

### Payload de Creación (`CreateScheduleBlockCommand`)
```json
{
  "employeeId": null,
  "startDateTime": "2026-11-21T14:00:00Z",
  "endDateTime": "2026-11-21T16:00:00Z",
  "reason": "Mantenimiento preventivo de equipo"
}
```

### Respuesta de Consulta (`ScheduleBlockDto`)
```json
{
  "id": 15,
  "businessId": "b8f2d1e0-3c4a-4a5e-b9f1-7c8d9e0f1a2b",
  "employeeId": null,
  "startDateTime": "2026-11-21T14:00:00Z",
  "endDateTime": "2026-11-21T16:00:00Z",
  "reason": "Mantenimiento preventivo de equipo"
}
```

---

## 🗄️ Entidades y Tablas Involucradas
* [[Entidad ScheduleBlock]] ➡️ Tabla `"bloqueos_agenda"`
* [[Entidad Business]] ➡️ Tabla `"negocios"`
* Persistencia gestionada mediante [[AppDbContext]], `IScheduleBlockRepository` y [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]].

---

## 🛡️ Seguridad y Aislamiento por Negocio (US-022)
* El frontend **no envía ni puede forzar un `businessId`** en el cuerpo de creación ni en la query de consulta.
* En la eliminación (`DELETE`), se comprueba estrictamente en base de datos que el bloqueo pertenezca al negocio del usuario que ejecuta la petición.

---

## 🧪 Pruebas Automatizadas Existentes
* **`ProtectedEndpointsTests.cs` (Integración):**
  - Confirma que cualquier petición sin token Bearer válido es interceptada con `401 Unauthorized`.
  - Confirma que un token con negocio alterado es rechazado con `403 Forbidden`.

---

## 🔗 Trazabilidad con la Bóveda
* **Historias de Usuario:** [[US-008 - Bloqueo Manual de Horarios y Disponibilidad]]
* **Entidades:** [[Entidad ScheduleBlock]], [[Entidad Business]], [[Entidad Appointment]]
* **Capa Arquitectura:** [[Modulo API]], [[Modulo Application]], [[Pipeline Behaviors]]
* **Persistencia:** [[AppDbContext]], [[Repositorios]], [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]]
* **Flujos:** [[Flujo de Bloqueo de Horarios]], [[Flujo General de Peticion]]
