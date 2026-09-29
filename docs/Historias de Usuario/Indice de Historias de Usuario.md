---
title: Indice de Historias de Usuario
tags:
  - historias-usuario
  - scrum
  - requerimientos
  - slotify
---

# 📋 Índice de Historias de Usuario (Product Backlog & Sprints)

Este directorio documenta formalmente las **Historias de Usuario** del sistema Slotify, estructuradas bajo el estándar Ágil (*Como/Quiero/Para*), con Criterios de Aceptación (*Given/When/Then - Gherkin*) y vinculación directa a los módulos de Backend y Frontend.

---

## 🧭 Tabla Resumen de Historias de Usuario

| Código | Título de la Historia | Alcance | Módulos Relacionados | Estado |
| :--- | :--- | :--- | :--- | :--- |
| **[[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes\|US-001]]** | Registro y Autenticación Multi-Inquilino | Auth & Tenants | AuthController, useAuthStore, Supabase | ✅ Implementado |
| **[[US-002 - Plantillas por Sector y Configuracion de Negocio\|US-002]]** | Selector de Plantillas por Sector | Onboarding B2B | SectorTemplatesController, 	emplates.tsx | ✅ Implementado |
| **[[US-003 - Calendario Tactil Multiformato y Consulta de Citas\|US-003]]** | Calendario Táctil Multiformato | Agenda & Citas | CalendarController, CalendarView, useCalendarStore | ✅ Implementado |
| **[[US-004 - Dashboard Dinamico y Manejo de Estados Vacios\|US-004]]** | Dashboard Dinámico & Estado Vacío | Dashboard B2B | DashboardScreen, CalendarController | ✅ Implementado |
| **[[US-005 - Micro-Onboarding para Consumidor Final\|US-005]]** | Micro-Onboarding para Consumidor | Onboarding B2C | ConsumerOnboardingScreen, RegisterClientScreen | ✅ Implementado |
| **[[US-008 - Bloqueo Manual de Horarios y Disponibilidad\|US-008]]** | Bloqueo Manual de Horarios | Agenda & Bloqueos | ScheduleBlocksController, BlockSlotModal | ✅ Implementado |

---

## 🔗 Relaciones con la Arquitectura
* [[Flujo General de Peticion]]
* [[Flujo de Consulta de Citas y Estado Vacio]]
* [[Flujo de Registro y Sync Cliente Consumidor]]
* [[Flujo de Registro y Sync Auth]]
