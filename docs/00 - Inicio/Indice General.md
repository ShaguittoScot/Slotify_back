---
title: Índice General - Slotify Backend
tags:
  - slotify
  - backend
  - moc
  - dashboard
---

# 📚 Slotify Backend - Knowledge Hub

Bienvenido a la base de conocimiento interactiva de **Slotify Backend**. En **Obsidian**, abre la **Vista de Grafo** (Ctrl + G) para visualizar cómo interactúan todos los módulos, entidades, historias de usuario y flujos de información del sistema.

---

## 🧭 Mapa de Navegación

### 📋 Historias de Usuario (Scrum / Requerimientos)
- [[Indice de Historias de Usuario]]: Directorio completo de User Stories y Criterios de Aceptación Gherkin.
  - [[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes|US-001: Registro y Autenticación]]
  - [[US-002 - Plantillas por Sector y Configuracion de Negocio|US-002: Plantillas por Sector]]
  - [[US-003 - Calendario Tactil Multiformato y Consulta de Citas|US-003: Calendario Táctil y Citas]]
  - [[US-004 - Dashboard Dinamico y Manejo de Estados Vacios|US-004: Dashboard y Estado Vacío]]
  - [[US-005 - Micro-Onboarding para Consumidor Final|US-005: Micro-Onboarding Consumidor]]
  - [[US-008 - Bloqueo Manual de Horarios y Disponibilidad|US-008: Bloqueo de Horarios]]

---

### 🏛️ Módulos de la Arquitectura
- [[Modulo API]]: Controladores HTTP, middlewares, configuración de JWT y OpenAPI.
- [[Modulo Application]]: Casos de uso, CQRS, comandos, queries, validaciones y DTOs.
- [[Modulo Domain]]: El núcleo del negocio, entidades puras e interfaces de repositorios.
- [[Modulo Infrastructure]]: Implementación de base de datos con EF Core, configuración Fluent API y Unit of Work.
- [[Pipeline Behaviors]]: Interceptores de MediatR para validación automática y logging.

---

### 🔄 Flujos de Información (End-to-End)
- [[Flujo General de Peticion]]: El viaje completo de un request desde el cliente hasta la BD y de vuelta.
- [[Flujo de Consulta de Citas y Estado Vacio]]: Ciclo de consulta y renderizado de agenda con manejo de estados vacíos.
- [[Flujo de Registro y Sync Cliente Consumidor]]: Sincronización B2C para clientes y flujo de onboarding.
- [[Flujo de Registro y Sync Auth]]: Sincronización B2B para administradores y creación de negocios.
- [[Flujo de Bloqueo de Horarios]]: Creación, consulta y eliminación de bloqueos de agenda.

---

### 📦 Módulos Funcionales (Features)
- [[Feature Auth]]: Autenticación de dueños y clientes con sincronización.
- [[Feature ScheduleBlocks]]: Bloqueo manual de horarios no disponibles.
- [[Feature Calendar]]: Consultas de disponibilidad de turnos y agenda táctil.
- [[Feature SectorTemplates]]: Plantillas predefinidas por rubro comercial.

---

### 🧩 Entidades y Modelo de Dominio
- [[Entidad User]] | [[Entidad Business]] | [[Entidad ScheduleBlock]]
- [[Entidad Appointment]] | [[Entidad AvailabilitySchedule]] | [[Entidad SectorTemplate]]

---

### 🗄️ Persistencia y Acceso a Datos
- [[AppDbContext]]: Configuración del contexto de Entity Framework Core.
- [[Unit of Work]]: Patrón de unidad de trabajo para consistencia transaccional.
- [[Repositorios]]: Implementaciones de acceso a datos.
- [[Supabase PostgreSQL]]: Base de datos relacional en la nube.
