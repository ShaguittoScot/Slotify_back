---
title: Feature Auth (Autenticacion y Registro)
tags:
  - feature
  - auth
  - login
  - sync
  - cliente
  - admin
---

# 🔐 Feature Auth

Ubicación en el código: src/Slotify.Application/Features/Auth/

Gestiona el registro inicial y sincronización tanto de **Administradores de Negocio** (DUENO) como de **Clientes Consumidores** (CLIENTE) después de autenticarse en Supabase Auth.

---

## 📂 Estructura de Archivos

### 1. Administradores de Negocio (DUENO)
* **Comandos:**
  * RegisterAdminCommand.cs: DTO de entrada para registrar el administrador y su negocio (Email, Name, BusinessName, SectorTemplateId).
* **Handlers:**
  * RegisterAdminHandler.cs: Ejecuta la creación atómica del usuario y el negocio.
* **Validadores:**
  * RegisterAdminValidator.cs: Reglas con FluentValidation.
* **DTOs:**
  * AuthUserDto.cs: Modelo de salida con datos del usuario y negocio creados.

### 2. Clientes Consumidores (CLIENTE)
* **Comandos:**
  * RegisterClientCommand.cs: DTO de entrada para sincronizar el cliente (Id, FirstName, LastName, Email, Phone).
* **Handlers:**
  * RegisterClientHandler.cs: Registra o actualiza el cliente en la tabla clientes.
* **Validadores:**
  * RegisterClientValidator.cs: Valida formato de correo y nombres requeridos.
* **DTOs:**
  * ClientDto.cs: Modelo de respuesta para clientes sincronizados.

---

## 🌐 Endpoints Expuestos en AuthController.cs

| Método | Ruta | Descripción | Payload |
| :--- | :--- | :--- | :--- |
| POST | /api/auth/sync | Sincroniza Administrador y crea Negocio. | RegisterAdminCommand |
| POST | /api/auth/sync-client | Sincroniza Cliente consumidor final. | RegisterClientCommand |

---

## 🔗 Relaciones en el Grafo
- **Invocado por:** [[Modulo API]] (AuthController)
- **Interactúa con:** [[Entidad User]], [[Entidad Business]], Client.cs, [[Entidad SectorTemplate]]
- **Flujos:**
  - [[Flujo de Registro y Sync Auth]]
  - [[Flujo de Registro y Sync Cliente Consumidor]]
- **Persistencia:** [[Unit of Work]], IClientRepository, [[Supabase PostgreSQL]]
