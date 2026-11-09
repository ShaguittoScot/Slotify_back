---
title: Flujo de Bloqueo de Horarios
tags:
  - flujo
  - agenda
  - turnos
---

# 📅 Flujo de Bloqueo de Horarios (`ScheduleBlocks`)

Permite a un negocio bloquear franjas de tiempo para que ningún cliente pueda agendar citas en ese periodo.

---

## 🔄 Diagrama de Secuencia: Creación de Bloqueo

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 👤 Administrador
    participant API as 🌐 ScheduleBlocksController
    participant Val as 🛡️ CreateScheduleBlockValidator
    participant Handler as ⚙️ CreateScheduleBlockHandler
    participant Repo as 📂 ScheduleBlockRepository
    participant DB as ☁️ Supabase PostgreSQL

    Admin->>API: POST /api/schedule-blocks { businessId, startDate, endDate, reason }
    API->>Val: Validación de campos y fechas UTC
    alt Fecha inicio >= Fecha fin
        Val-->>API: Error: StartDate debe ser anterior a EndDate
        API-->>Admin: 400 Bad Request
    else Validación exitosa
        API->>Handler: MediatR.Send(CreateScheduleBlockCommand)
        Handler->>Repo: AddAsync([[Entidad ScheduleBlock]])
        Handler->>DB: SaveChangesAsync()
        DB-->>Handler: Registro guardado (Id asignado)
        Handler-->>API: ResultResponse<long>
        API-->>Admin: 201 Created { id: 123 }
    end
```

---

## 🔍 Consulta de Bloqueos (`GET /api/schedule-blocks`)

Cuando el calendario del negocio se abre:
1. El cliente envía `GET /api/schedule-blocks?startDate=...&endDate=...`.
2. `GetScheduleBlocksHandler` invoca `ScheduleBlockRepository.GetByDateRangeAsync()`.
3. Entity Framework consulta la tabla `ScheduleBlocks` filtrando por `StartDate >= @start AND EndDate <= @end`.
4. Se retornan los bloqueos mapeados a `ScheduleBlockDto`.

---

## 🔗 Componentes Relacionados
* [[Feature ScheduleBlocks]]
* [[Entidad ScheduleBlock]]
* [[Entidad Business]]
* [[Repositorios]]
