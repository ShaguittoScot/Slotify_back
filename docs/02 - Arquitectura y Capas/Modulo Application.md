---
title: Módulo Application (Slotify.Application)
tags:
  - modulo
  - application
  - cqrs
  - mediatr
  - logic
---

# Módulo Application (`Slotify.Application`)

La capa **`Slotify.Application`** contiene la **lógica de negocio y casos de uso** del sistema. No conoce detalles sobre controladores HTTP ni sobre Entity Framework Core directo, garantizando un código desacoplado y altamente testeable.

---

## Responsabilidades del Módulo
1. **Patrón CQRS (Command Query Responsibility Segregation):**
   - **Commands (Escritura):** Modifican el estado del sistema (`RegisterAdminCommand`, `RegisterClientCommand`, `ChangeBusinessTemplateCommand`, `UpdateBusinessFormConfigCommand`, `CreateScheduleBlockCommand`, `DeleteScheduleBlockCommand`).
   - **Queries (Lectura):** Consultan datos sin alterar el estado (`GetScheduleBlocksQuery`, `GetCalendarSlotsQuery`, `GetSectorTemplatesQuery`, `GetBusinessBookingConfigQuery`).
2. **Handlers de MediatR:** Cada caso de uso tiene su propio *Handler* con una única responsabilidad.
3. **Validación de Entrada:** Validadores automáticos construidos con **FluentValidation** (`RegisterAdminValidator`, `RegisterClientValidator`, `CreateScheduleBlockValidator`).
4. **Transformación de Datos (DTOs):** Mapeo de entidades de dominio hacia objetos de transferencia seguros (`AuthUserDto`, `ClientDto`, `CalendarSlotsDto`, etc.).

---

## Submódulos Funcionales (Features)
* [[Feature Auth]]: Registro y sincronización de administradores y clientes.
* [[Feature Businesses]]: Catálogo público, cambio de plantillas de rubro y configuración de formularios dinámicos.
* [[Feature ScheduleBlocks]]: Gestión de bloqueos de agenda y excepciones.
* [[Feature Calendar]]: Consultas de disponibilidad de turnos con cálculo en tiempo real.
* [[Feature SectorTemplates]]: Consulta de plantillas de rubro comercial.

---

## 🔗 Relaciones en el Grafo
- **Depende de:** [[Modulo Domain]]
- **Es consumido por:** [[Modulo API]]
- **Utiliza abstracciones de:** [[Unit of Work]], [[Repositorios]]
- **Flujos:** [[Flujo General de Peticion]], [[Flujo de Bloqueo de Horarios]], [[Flujo de Registro y Sync Auth]], [[Flujo de Registro y Sync Cliente Consumidor]]

```mermaid
graph LR
    API["🌐 [[Modulo API]]"] --> CQRS["🔀 Commands / Queries"]
    CQRS --> Validation["🛡️ FluentValidation"]
    Validation --> Handlers["⚙️ Feature Handlers"]
    Handlers --> UOW["🗄️ [[Unit of Work]]"]
    Handlers --> Domain["💎 [[Modulo Domain]]"]
```
