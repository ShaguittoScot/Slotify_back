---
title: US-008 - Bloqueo Manual de Horarios y Disponibilidad
aliases:
  - US-008
  - US-023
  - Bloqueos de Horario
tags:
  - historia-usuario
  - bloqueos
  - agenda
  - disponibilidad
  - us-008
estado_documental: "Original verificada"
implementacion_backend: "Completa"
integracion_frontend: "No integrada / Stubs"
---

# ⛔ US-008: Bloqueo Manual de Horarios y Disponibilidad

* **Como:** Dueño de negocio o empleado autorizado.
* **Quiero:** Registrar y remover bloqueos de tiempo en la agenda operativa para descansos, comidas, citas externas o mantenimiento.
* **Para:** Prevenir reservas en intervalos no disponibles y liberar franjas horarias cuando la agenda cambie.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Creación de bloqueo horario manual
```gherkin
Dado que el usuario autenticado abre el modal de bloqueo desde la agenda
Cuando especifica startDateTime, endDateTime y motivo (reason)
Entonces el frontend envía POST /api/schedule-blocks
Y el backend resuelve el BusinessId del JWT del usuario
Y valida que endDateTime sea posterior a startDateTime y pertenezcan al mismo día
Y persiste el registro en la tabla bloqueos_agenda
Y el slot bloqueado se refleja en la consulta de calendario con tipo "block".
```

### Escenario 2: Eliminación de un bloqueo horario (desbloquear)
```gherkin
Dado que un bloqueo existe en la agenda del negocio
Cuando el administrador solicita eliminar dicho bloqueo
Entonces el sistema envía DELETE /api/schedule-blocks/{id}
Y el backend valida que el bloqueo pertenezca al negocio del usuario autenticado (403 si no coincide)
Y elimina el registro de bloqueos_agenda liberando la franja horaria correspondiente.
```

---

## ⚙️ Implementación Backend Real

* **Controlador:** `ScheduleBlocksController.cs`
* **Rutas HTTP:**
  - `GET /api/schedule-blocks`
  - `POST /api/schedule-blocks`
  - `DELETE /api/schedule-blocks/{id}`
* **Comandos y Consultas:**
  - `GetScheduleBlocksQuery` / `GetScheduleBlocksHandler`
  - `CreateScheduleBlockCommand` / `CreateScheduleBlockHandler`
  - `DeleteScheduleBlockCommand` / `DeleteScheduleBlockHandler`
* **Validador:** `CreateScheduleBlockValidator`
* **Entidades Involucradas:** [[Entidad ScheduleBlock]], [[Entidad Business]]
* **Tabla de Base de Datos Impactada:** `bloqueos_agenda`

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `BlockSlotModal.tsx`, `CalendarView.tsx`
* **Capa API:** `src/features/schedule-blocks/api/index.ts`
* **Nivel de Integración:** No integrada / Stubs con errores.
* **Detalle Crítico:**
  - Aunque el componente visual `BlockSlotModal.tsx` está diseñado, las funciones en `src/features/schedule-blocks/api/index.ts` no ejecutan llamadas HTTP reales y arrojan errores intencionales:
    - `throw new Error('US-008: createScheduleBlock no implementado');`
    - `throw new Error('US-008: deleteScheduleBlock no implementado');`
  - El backend cuenta con la API completa, validada y probada; se requiere conectar las funciones del frontend a `POST /api/schedule-blocks` y `DELETE /api/schedule-blocks/{id}`.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas de Repositorio:** `ScheduleBlockRepositoryTests.cs`.
* **Pruebas de Integración:** `ProtectedEndpointsTests.cs` (creación, listado y borrado con token multi-tenant).

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-003 - Calendario Tactil Multiformato y Consulta de Citas]]
* [[Feature ScheduleBlocks]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad ScheduleBlock]]
* [[Entidad Business]]
* [[Flujo de Bloqueo de Horarios]]
