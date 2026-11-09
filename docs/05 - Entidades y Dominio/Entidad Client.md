---
title: Entidad Client
tags:
  - domain
  - entity
  - client
  - b2c
---

# 👤 Entidad `Client`

Ubicación: `src/Slotify.Domain/Entities/Client.cs`  
Tabla en PostgreSQL: `"clientes"`

Representa a los clientes consumidores finales (B2C) dentro del ecosistema de Slotify. Su identificador principal `Id` (Guid) corresponde con el UUID de autenticación de Supabase (`auth.users.id`).

---

## 📋 Propiedades Principales
* **`Id` (`Guid`):** Clave primaria, vinculada al ID de Supabase Auth.
* **`SupabaseId` (`Guid?`):** Identificador opcional para enlace con Supabase Auth (Índice único).
* **`FirstName` (`string`):** Nombre de pila del cliente (Columna `nombre`).
* **`LastName` (`string`):** Apellido del cliente (Columna `apellido`).
* **`FullName` (`string`):** Propiedad calculada (`FirstName + LastName`), ignorada en base de datos.
* **`Email` (`string`):** Correo electrónico del cliente (Columna `correo`, Único).
* **`Phone` (`string?`):** Teléfono o WhatsApp de contacto (Columna `telefono`).
* **`CreatedAt` / `UpdatedAt` (`DateTime`):** Auditoría automática (`fecha_creacion`, `fecha_actualizacion`).

---

## 🔗 Relaciones en el Grafo
- **Pertenece a:** [[Modulo Domain]]
- **Relacionado con:** [[Entidad Appointment]]
- **Configuración EF Core:** `ClientConfiguration.cs` -> [[AppDbContext]]
- **Usado en:** [[Feature Auth]], [[Flujo de Registro y Sync Cliente Consumidor]], [[Flujo General de Peticion]]
