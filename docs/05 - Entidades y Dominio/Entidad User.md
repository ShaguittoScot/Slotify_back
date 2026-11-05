---
title: Entidad User
tags:
  - domain
  - entity
  - user
---

# 👤 Entidad `User`

Ubicación: `src/Slotify.Domain/Entities/User.cs`  
Tabla en PostgreSQL: `"Users"`

Representa a los usuarios dentro del sistema. Su identificador principal `Id` (Guid) corresponde con el UUID de autenticación de Supabase.

---

## 📋 Propiedades Principales
* **`Id` (`Guid`):** Clave primaria, vinculada al ID de Supabase Auth.
* **`Email` (`string`):** Correo electrónico del usuario (Único).
* **`Name` (`string`):** Nombre completo.
* **`Phone` (`string?`):** Teléfono de contacto opcional.
* **`Role` (`string`):** Rol del usuario (`DUENO`, `EMPLEADO`, `CLIENTE`).
* **`IsActive` (`bool`):** Estado del usuario.
* **`CreatedAt` / `UpdatedAt` (`DateTime`):** Auditoría automática en UTC.

---

## 🔗 Relaciones en el Grafo
- **Pertenece a:** [[Modulo Domain]]
- **Relacionado con:** [[Entidad Business]], [[Entidad Appointment]]
- **Usado en:** [[Feature Auth]], [[Flujo de Registro y Sync Auth]]
