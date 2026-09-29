---
title: Entidad Business
tags:
  - domain
  - entity
  - business
---

# 🏢 Entidad `Business`

Ubicación: `src/Slotify.Domain/Entities/Business.cs`  
Tabla en PostgreSQL: `"Businesses"`

Representa un negocio o establecimiento comercial registrado en Slotify.

---

## 📋 Propiedades Principales
* **`Id` (`Guid`):** Clave primaria.
* **`Name` (`string`):** Nombre comercial del negocio.
* **`Phone` (`string?`):** Teléfono del negocio.
* **`Address` (`string?`):** Dirección física.
* **`OwnerId` (`Guid`):** FK hacia [[Entidad User]] (Dueño).
* **`SectorTemplateId` (`Guid?`):** FK opcional hacia [[Entidad SectorTemplate]].

---

## 🔗 Relaciones en el Grafo
- **Pertenece a:** [[Modulo Domain]]
- **Relacionado con:** [[Entidad User]], [[Entidad ScheduleBlock]], [[Entidad Appointment]], [[Entidad AvailabilitySchedule]], [[Entidad SectorTemplate]]
- **Usado en:** [[Feature Auth]], [[Feature ScheduleBlocks]], [[Feature Calendar]]
