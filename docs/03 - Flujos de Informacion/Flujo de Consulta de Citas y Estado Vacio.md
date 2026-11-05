---
title: Flujo de Consulta de Citas y Estado Vacio
tags:
  - flujo
  - calendario
  - citas
  - dashboard
  - frontend-backend
---

# 📅 Flujo de Consulta de Citas y Manejo de Estados Vacíos

Este documento describe el flujo integral desde la **Historia de Usuario** hasta el tránsito de datos entre los módulos del **Frontend (React Native / Expo)** y el **Backend (.NET 8 Web API / Supabase PostgreSQL)**.

---

## 🎯 Historia de Usuario Asociada: US-003 / US-004

* **Como:** Dueño de negocio o empleado autenticado en Slotify.
* **Quiero:** Consultar las citas registradas en tiempo real para mi negocio y disponer de una interfaz clara cuando aún no tenga citas programadas.
* **Para:** Administrar la operatividad diaria, dar seguimiento a clientes y crear nuevas reservas fácilmente.

### Criterios de Aceptación:
1. **Aislamiento Demo:** La cuenta demo@slotify.com / dmin@slotify.com renderiza datos simulados offline.
2. **Cuentas Reales:** Consulta el endpoint GET /api/calendar/slots autenticado vía Bearer JWT (usiness_id).
3. **Manejo de Estado Vacío:** Si el backend responde sin citas (slots: []), el frontend muestra un componente de estado vacío con botones de acción rápida (*Crear Cita*, *Compartir Enlace*) y métricas en cero sin provocar errores.

---

## 📊 Diagrama de Secuencia End-to-End

`mermaid
sequenceDiagram
    autonumber
    actor Admin as Administrador / Dueño
    participant UI as Dashboard / CalendarView (Frontend)
    participant Store as useCalendarStore (Zustand)
    participant Client as apiClient (Axios Interceptor)
    participant API as CalendarController (.NET 8)
    participant MediatR as GetCalendarSlotsQueryHandler
    participant DB as Supabase PostgreSQL

    Admin->>UI: Ingresa a la pantalla
    UI->>Store: loadSlots()
    Store->>Client: GET /api/calendar/slots?startDate=...&endDate=...
    
    alt Cuenta Demo (demo@slotify.com)
        Client-->>Store: Retorna mockCalendarSlots (Offline)
        Store-->>UI: Renderiza citas de demostración
    else Cuenta Real Registrada
        Client->>API: HTTP GET (Authorization: Bearer <JWT>)
        API->>MediatR: Send(GetCalendarSlotsQuery)
        MediatR->>DB: SELECT * FROM citas WHERE id_negocio = @bizId AND fecha_inicio BETWEEN @start AND @end
        DB-->>MediatR: Dataset de Citas y Bloqueos
        MediatR-->>API: ResultResponse<CalendarResponse>
        API-->>Client: 200 OK JSON { rangeStart, rangeEnd, slots: [] }
        
        alt Existen Citas
            Client-->>Store: slots con datos reales
            Store-->>UI: Renderiza tarjetas de citas y calcula métricas
        else No existen citas registradas
            Client-->>Store: slots: []
            Store-->>UI: Renderiza EmptyStateCard (Aún no tienes citas registradas)
        end
    end
`

---

## 🔄 Tránsito de la Información por Capas

### 1. Capa de Presentación (Frontend)
* **Componentes:** DashboardScreen, CalendarView, EmptyStateCard.
* **Comportamiento:** Escucha reactivamente el estado de slots en useCalendarStore. Si slots.length === 0, renderiza el mensaje informativo con enlaces de acción.

### 2. Capa ViewModel / Store (Frontend)
* **Archivo:** src/features/calendar/model/index.ts
* **Acción:** Invoca loadSlots(), calcula métricas en tiempo real (Citas Hoy, Clientes, Ingresos Hoy, Pendientes).

### 3. Capa de Red (API Client)
* **Archivo:** src/features/calendar/api/index.ts
* **Responsabilidad:** Gestiona la llamada a GET /api/calendar/slots. Para cuentas no-demo, propaga la respuesta real del backend sin inyectar datos falsos.

### 4. Capa de Controladores (Backend)
* **Controlador:** CalendarController.cs
* **Endpoint:** [HttpGet(slots)]
* **Seguridad:** Extrae el BusinessId del claim del JWT del usuario autenticado (*Zero Trust*).

### 5. Capa de Aplicación (Backend MediatR Handler)
* **Query:** GetCalendarSlotsQuery
* **Handler:** GetCalendarSlotsQueryHandler
* **Lógica:** Consulta las citas activas y bloqueos del negocio para el rango de fechas solicitado.

### 6. Capa de Persistencia (Base de Datos)
* **Tablas involucradas:** citas, servicios, loqueos_agenda, 
egocios.
* **Filtro Multi-Tenant:** WHERE id_negocio = @businessId.

---

## 🔗 Enlaces Relacionados
* [[Flujo General de Peticion]]
* [[Feature Calendar]]
* [[Base de datos - Proyecto]]
