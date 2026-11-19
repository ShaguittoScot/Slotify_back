---
title: Indice de Historias de Usuario
aliases:
  - Indice de Historias de Usuario
  - Product Backlog
  - User Stories
tags:
  - historias-usuario
  - scrum
  - requerimientos
  - slotify
  - backlog
---

# 📋 Índice de Historias de Usuario (Product Backlog & Trazabilidad)

> [!abstract] Resumen del Backlog
> Este directorio documenta las **Historias de Usuario (US)** del sistema Slotify Backend y su correspondencia con la aplicación frontend. Cada historia se encuentra formalizada bajo el estándar Ágil (*Como / Quiero / Para*), con Criterios de Aceptación (*Given / When / Then - Gherkin*), trazabilidad a los endpoints reales, comandos CQRS, entidades y estado de integración.

---

## 🧭 Matriz Consolidada de Historias de Usuario

| Código | Título de la Historia | Clasificación Documental | Implementación Backend | Integración Frontend | Componentes y Endpoints Clave |
| :--- | :--- | :--- | :---: | :---: | :--- |
| **[[US-000 - Autenticacion y Registro de Administrador]]** | Autenticación y Registro de Administrador | Original verificada | ✅ Completa | ⚠️ Parcial | `AuthController` (`POST /api/auth/register-admin`, `POST /api/auth/login`) |
| **[[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes]]** | Registro y Autenticación Multi-Inquilino y Clientes | Original verificada | ✅ Completa | ⚠️ Discrepante | `AuthController` (`POST /api/auth/sync`, `/sync-client`, `/exchange-token`) |
| **[[US-002 - Plantillas por Sector y Configuracion de Negocio]]** | Plantillas por Sector y Configuración de Negocio | Original verificada | ✅ Completa | ⚠️ Discrepante | `SectorTemplatesController` (`GET /api/sector-templates`), `BusinessesController` |
| **[[US-003 - Calendario Tactil Multiformato y Consulta de Citas]]** | Calendario Táctil Multiformato y Consulta de Citas | Original verificada | ✅ Completa | ⚠️ Parcial | `CalendarController` (`GET /api/calendar/slots`) |
| **[[US-004 - Dashboard Dinamico y Manejo de Estados Vacios]]** | Dashboard Dinámico y Manejo de Estados Vacíos | Original verificada | ℹ️ Indirecta (Cliente) | ✅ Integrada | `DashboardScreen`, `EmptyStateCard` (cálculo sobre `GET /api/calendar/slots`) |
| **[[US-005 - Micro-Onboarding para Consumidor Final]]** | Micro-Onboarding para Consumidor Final (B2C) | Original verificada | ✅ Completa | ✅ Integrada | `AuthController` (`POST /api/auth/sync-client`), `ConsumerOnboardingScreen` |
| **[[US-006 - Selector Inicial de Giro Comercial]]** | Selector Inicial de Giro Comercial (Wizard B2B) | Original verificada | ✅ Completa | ✅ Integrada | `SectorTemplatesController` (`GET /api/sector-templates`), `SectorSelectionScreen` |
| **[[US-008 - Bloqueo Manual de Horarios y Disponibilidad]]** | Bloqueo Manual de Horarios y Disponibilidad | Original verificada | ✅ Completa | ❌ Stubs con error | `ScheduleBlocksController` (`GET`, `POST`, `DELETE /api/schedule-blocks`) |
| **[[US-011 - Agendamiento y Creacion de Citas]]** | Agendamiento y Creación de Citas | Reconstruida desde código | ✅ Completa | ❌ No integrada | `CalendarController` (`POST /api/calendar/appointments`), `CreateAppointmentCommand` |
| **[[US-013 - Cambio de Plantilla en Negocio Existente]]** | Cambio de Plantilla en Negocio Existente | Reconstruida desde código | ✅ Completa | ⚠️ Discrepante | `BusinessesController` (`PUT /api/businesses/{id}/template`) |
| **[[US-017 - Onboarding Transaccional B2B y Formulario Dinamico]]** | Onboarding Transaccional B2B y Formulario Dinámico | Reconstruida desde código | ✅ Completa | ⚠️ Discrepante | `OnboardingController` (`POST .../complete-business-onboarding`), `BusinessesController` |
| **[[US-022 - Aislamiento Multi-Inquilino por Token JWT]]** | Aislamiento Multi-Inquilino por Token JWT | Reconstruida desde código | ✅ Completa | ⛔ Bloqueante | `CurrentUserService`, `JwtService`, middleware `JwtBearer`, claim `businessId` |
| **[[US-023 - Detalle Publico de Negocios por Slug]]** | Detalle Público de Negocios por Slug | Reconstruida desde implementación | ✅ Completa | ✅ Integrada | `BusinessesController` (`GET /api/businesses/{slug}`), `GetBusinessBySlugHandler` |

---

## 🏷️ Criterios de Clasificación Documental

1. **Original verificada (7 historias):**
   - Historias definidas explícitamente en la documentación de producto, tipos compartidos o requerimientos de usuario del frontend (`US-000`, `US-001`, `US-002`, `US-003`, `US-004`, `US-005`, `US-006`, `US-008`).
2. **Reconstruida desde implementación (4 historias + 1 técnica):**
   - Funcionalidades implementadas en controladores, comandos MediatR, entidades y pruebas de integración sin una ficha documental aislada previa en el backend (`US-011`, `US-013`, `US-017`, `US-022`). Todas incluyen el callout de advertencia estándar:
     > `> [!warning] Reconstruida a partir del código. Pendiente de validación funcional.`

---

## 🔄 Principales Discrepancias Frontend / Backend

* **Autenticación e Intercambio de Tokens ([[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes]], [[US-022 - Aislamiento Multi-Inquilino por Token JWT]]):** El frontend consume Supabase Auth pero envía directamente el token de Supabase al backend, en lugar de invocar `POST /api/auth/exchange-token` para obtener el JWT de Slotify con el claim `businessId`.
* **Cambio de Giro Comercial ([[US-002 - Plantillas por Sector y Configuracion de Negocio]], [[US-013 - Cambio de Plantilla en Negocio Existente]]):** El frontend llama a `PATCH /businesses/{id}/sector` con `{ sector: string }`, mientras que el backend expone `PUT /api/businesses/{id}/template` con `{ sectorTemplateId: number }`.
* **Stubs en Bloqueos de Horario ([[US-008 - Bloqueo Manual de Horarios y Disponibilidad]]):** El frontend mantiene stubs en `src/features/schedule-blocks/api/index.ts` que lanzan excepciones explícitas en lugar de llamar a `POST /api/schedule-blocks` y `DELETE /api/schedule-blocks/{id}`.
* **Creación de Citas ([[US-011 - Agendamiento y Creacion de Citas]]):** El frontend no tiene conectado el formulario de creación de citas con el endpoint `POST /api/calendar/appointments`.

---

## 🔗 Relaciones con la Arquitectura

* [[Flujo General de Peticion]]
* [[Flujo de Consulta de Citas y Estado Vacio]]
* [[Flujo de Registro y Sync Cliente Consumidor]]
* [[Flujo de Registro y Sync Auth]]
* [[Flujo de Bloqueo de Horarios]]
* [[Contratos de API REST]]
* [[Modulo API]]
* [[Clean Architecture]]
