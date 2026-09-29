# 📘 Documentación Integral: Historias de Usuario y Flujo de Información (Frontend & Backend)

> **Proyecto:** Slotify — Plataforma Multi-Inquilino de Gestión de Citas, Agendas y Servicios  
> **Ubicación:** Slotify_back/docs/HISTORIAS_DE_USUARIO_Y_FLUJO_INTEGRAL.md  
> **Alcance:** Historias de Usuario, Criterios de Aceptación, Arquitectura por Capas (.NET 8 + React Native) y Flujo de Datos Extremo a Extremo.

---

## 📑 Tabla de Contenidos
1. [Historias de Usuario y Criterios de Aceptación](#1-historias-de-usuario-y-criterios-de-aceptación)
   - [US-001: Autenticación, Registro y Modo Demo](#us-001-autenticación-registro-y-modo-demo)
   - [US-002 / US-006: Plantillas por Sector y Configuración de Negocio](#us-002--us-006-plantillas-por-sector-y-configuración-de-negocio)
   - [US-003: Calendario Táctil Multiformato y Consulta de Citas](#us-003-calendario-táctil-multiformato-y-consulta-de-citas)
   - [US-004: Dashboard Dinámico y Manejo de Estados Vacíos](#us-004-dashboard-dinámico-y-manejo-de-estados-vacíos)
   - [US-008: Bloqueos de Horario y Disponibilidad](#us-008-bloqueos-de-horario-y-disponibilidad)
2. [Arquitectura del Sistema y Módulos](#2-arquitectura-del-sistema-y-módulos)
3. [Flujo de Información por Capas (Frontend a Backend)](#3-flujo-de-información-por-capas-frontend-a-backend)
   - [Diagrama de Secuencia General](#diagrama-de-secuencia-general)
   - [Flujo de Consulta de Citas (Cuentas Reales vs Demo)](#flujo-de-consulta-de-citas-cuentas-reales-vs-demo)
   - [Flujo de Creación y Agendamiento de Cita](#flujo-de-creación-y-agendamiento-de-cita)
4. [Estructura de Datos y DTOs](#4-estructura-de-datos-y-dtos)
5. [Seguridad y Aislamiento Multi-Inquilino](#5-seguridad-y-aislamiento-multi-inquilino)

---

## 1. Historias de Usuario y Criterios de Aceptación

### US-001: Autenticación, Registro y Modo Demo

* **Como:** Dueño de negocio o cliente.
* **Quiero:** Iniciar sesión con mis credenciales o explorar la aplicación en modo demo sin alterar la base de datos real.
* **Para:** Administrar mi negocio o agendar mis servicios de forma segura.

#### Criterios de Aceptación (Gherkin):
`gherkin
Escenario: Inicio de sesión con cuenta Demo
  Dado que el usuario ingresa el correo demo@slotify.com o admin@slotify.com
  Cuando presiona el botón Iniciar Sesión
  Entonces el sistema autentica de forma instantánea en modo DEMO local
  Y carga los datos simulados de demostración (citas, clientes y métricas de prueba)
  Sin requerir llamadas a la base de datos de producción.

Escenario: Inicio de sesión con cuenta Real
  Dado que un usuario ingresa sus credenciales registradas
  Cuando presiona Iniciar Sesión
  Entonces el sistema valida el token JWT con Supabase/Backend
  Y carga las citas reales vinculadas a su usiness_id
  Y si aún no tiene citas registradas, muestra el estado vacío informativo.
`

---

### US-002 / US-006: Plantillas por Sector y Configuración de Negocio

* **Como:** Nuevo dueño de negocio (*Admin/Dueño*).
* **Quiero:** Seleccionar mi giro comercial (Barbería, Spa/Belleza, Consultorio Dental, etc.) en el onboarding.
* **Para:** Que el sistema preconfigure automáticamente mis servicios, duraciones y horarios de atención por defecto.

#### Criterios de Aceptación:
1. Al registrarse un nuevo negocio, se consulta el catálogo de plantillas vía GET /api/sector-templates.
2. El usuario puede seleccionar una plantilla predefinida o personalizar servicios.
3. El backend persiste la vinculación en 
egocios.id_plantilla_sector e inserta los registros en servicios asociados al id_negocio.

---

### US-003: Calendario Táctil Multiformato y Consulta de Citas

* **Como:** Administrador o empleado del negocio.
* **Quiero:** Consultar la agenda en vistas de Día, Semana y Mes con actualización reactiva.
* **Para:** Visualizar las citas programadas, horas libres y bloqueos en tiempo real.

#### Criterios de Aceptación:
`gherkin
Escenario: Consulta de citas registradas en la agenda
  Dado que el administrador abre la pantalla de Agenda
  Cuando el componente CalendarView se monta o cambia de fecha
  Entonces invoca useCalendarStore.loadSlots()
  Y realiza la petición HTTP GET /api/calendar/slots?startDate=...&endDate=...
  Si la cuenta es real y no tiene citas, muestra la grilla horaria despejada con opción de agendar
  Si la cuenta es demo, renderiza los slots simulados del período.
`

---

### US-004: Dashboard Dinámico y Manejo de Estados Vacíos

* **Como:** Administrador del negocio.
* **Quiero:** Ver un resumen con las métricas del día (Citas Hoy, Clientes Únicos, Ingresos Estimados, Citas Pendientes) y la lista de próximas citas.
* **Para:** Tomar decisiones operativas rápidas y acceder a acciones clave.

#### Criterios de Aceptación:
1. **Conexión en Vivo:** El dashboard consulta las citas reales del negocio mediante el store del calendario.
2. **Cálculo Reactivo:** Si existen citas registradas para la fecha, calcula en tiempo real:
   - *Citas Hoy:* Sumatoria de citas activas del día.
   - *Clientes:* Conteo de clientes únicos atendidos.
   - *Ingresos Hoy:* Suma monetaria de los servicios confirmados/completados.
   - *Pendientes:* Citas en estado pending.
3. **Estado Vacío Amigable:** Si el negocio no tiene citas registradas:
   - Las métricas inician en 0 / .
   - La sección de Próximas Citas muestra una tarjeta ilustrada con el mensaje *Aún no tienes citas registradas*.
   - Provee botones directos para **Crear Cita** (navegación a agenda) y **Compartir Link**.

---

### US-008: Bloqueos de Horario y Disponibilidad

* **Como:** Dueño de negocio o empleado.
* **Quiero:** Bloquear intervalos de tiempo específicos (almuerzo, mantenimiento, descansos).
* **Para:** Evitar que los clientes reserven en franjas horarias no disponibles.

#### Criterios de Aceptación:
1. El modal de bloqueo permite seleccionar fecha, hora inicio, hora fin, motivo y si aplica a un empleado o a todo el personal.
2. Se envía la solicitud POST /api/schedule-blocks.
3. El calendario renderiza el slot con tipo lock, color distintivo y deshabilita reservas en ese rango.

---

## 2. Arquitectura del Sistema y Módulos

`mermaid
graph TD
    subgraph Frontend_Expo[Frontend (React Native + Expo SDK 54)]
        UI[Capa de Presentación / UI Screens\n(DashboardScreen, CalendarView, etc.)]
        VM[Capa ViewModel / Stores\n(useAuthStore, useCalendarStore)]
        API_LAYER[Capa de Servicios API / HTTP\n(calendarApi, authApi, sectorTemplatesApi)]
        HTTP_CLIENT[Cliente HTTP Interceptor\n(Axios + Bearer Token)]
    end

    subgraph Backend_API[Backend (.NET 8 Web API / MediatR)]
        CTRL[Controladores REST\n(CalendarController, AuthController)]
        PIPELINE[MediatR Pipeline Behaviors\n(Validation / Logging / Tenant)]
        HANDLERS[Application Handlers\n(GetCalendarSlotsQueryHandler)]
        DOMAIN[Dominio / Entidades\n(Appointment, Business, Service)]
        UOW[Unit of Work / Repositorios\n(Slotify.Infrastructure / EF Core 9)]
    end

    subgraph Database[Base de Datos (Supabase PostgreSQL Multi-tenant)]
        DB[(Tablas: negocios, citas,\nservicios, bloqueos_agenda)]
    end

    UI --> VM
    VM --> API_LAYER
    API_LAYER --> HTTP_CLIENT
    HTTP_CLIENT -->|HTTPS / REST| CTRL
    CTRL --> PIPELINE
    PIPELINE --> HANDLERS
    HANDLERS --> DOMAIN
    HANDLERS --> UOW
    UOW --> DB
`

---

## 3. Flujo de Información por Capas (Frontend a Backend)

### Diagrama de Secuencia General

`mermaid
sequenceDiagram
    autonumber
    actor Usuario as Administrador / Cliente
    participant Vista as UI Screen (Dashboard / Calendar)
    participant Store as ViewModel / Zustand Store
    participant API as Frontend API Layer (Axios)
    participant Backend as CalendarController (.NET 8)
    participant MediatR as GetCalendarSlotsQueryHandler
    participant DB as PostgreSQL Database

    Usuario->>Vista: Abre la aplicación / Pantalla
    Vista->>Store: dispatch loadSlots()
    Store->>API: fetchCalendarSlots(startDate, endDate)
    
    alt Usuario Autenticado Real
        API->>Backend: GET /api/calendar/slots?startDate=...&endDate=... (Bearer JWT)
        Backend->>MediatR: Send(GetCalendarSlotsQuery)
        MediatR->>DB: SELECT * FROM citas WHERE id_negocio = @bizId AND fecha_inicio BETWEEN @start AND @end
        DB-->>MediatR: Registros de citas y bloqueos
        MediatR-->>Backend: ResultResponse<CalendarResponse>
        Backend-->>API: 200 OK [CalendarResponse]
        
        alt Existen citas
            API-->>Store: Retorna slots reales
            Store-->>Vista: Renderiza slots y calcula métricas
        else No existen citas (Lista vacía)
            API-->>Store: Retorna [] (Empty Array)
            Store-->>Vista: Renderiza Estado Vacío (Aún no tienes citas registradas)
        end

    else Usuario Demo (demo@slotify.com)
        API-->>Store: Retorna datos simulados (generateMockCalendarSlots)
        Store-->>Vista: Renderiza slots de demostración y métricas demo
    end
`

---

## 4. Estructura de Datos y DTOs

### Modelo de Slot Unificado (CalendarSlot):
`	ypescript
export interface CalendarSlot {
  resourceId: string;
  type: 'appointment' | 'block' | 'available';
  startTime: string; // ISO 8601 (ej. 2026-09-29T10:00:00.000Z)
  endTime: string;   // ISO 8601
  title: string;
  status: 'confirmed' | 'pending' | 'cancelled' | 'completed' | 'blocked';
  clientName?: string;
  clientPhone?: string;
  servicePrice?: string;
  employeeName?: string;
  blockReason?: string;
}
`

### Contrato de Respuesta del Backend (CalendarResponse):
`	ypescript
export interface CalendarResponse {
  rangeStart: string;
  rangeEnd: string;
  slots: CalendarSlot[];
}
`

---

## 5. Seguridad y Aislamiento Multi-Inquilino

1. **Principio Zero-Trust:** El backend nunca confía en el usiness_id enviado en el cuerpo de la petición. Este identificador se extrae directamente del **Token JWT verificado** en los claims de autorización.
2. **Aislamiento por Negocio (*Multi-Tenant Isolation*):** Cada consulta SQL filtra estrictamente por id_negocio = @authenticatedUserBusinessId.
3. **Protección de Almacenamiento Local:** Los tokens de acceso se almacenan en almacenamiento seguro nativo (SecureStore en móvil).

---

> 📌 **Mantenimiento:** Este documento forma parte de la bóveda de documentación en Slotify_back/docs/.
