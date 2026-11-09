---
title: Entidad SectorTemplate
tags:
  - domain
  - entity
  - templates
---

# 📋 Entidad `SectorTemplate`

Ubicación: `src/Slotify.Domain/Entities/SectorTemplate.cs`  
Tabla en PostgreSQL: `"SectorTemplates"`

Define plantillas por sector comercial (salud, estética, servicios profesionales) que facilitan la parametrización rápida de nuevos negocios.

---

## 📋 Propiedades Principales
* **`Id` (`Guid`):** Clave primaria.
* **`Name` (`string`):** Nombre del sector (ej. "Barbería / Peluquería").
* **`Description` (`string?`):** Descripción del rubro.
* **`DefaultSlotDurationMinutes` (`int`):** Duración estándar de los turnos en minutos.
* **`IsActive` (`bool`):** Estado de la plantilla.

---

## 🔗 Relaciones en el Grafo
- **Pertenece a:** [[Modulo Domain]]
- **Relacionado con:** [[Entidad Business]]
- **Usado en:** [[Feature SectorTemplates]], [[Feature Auth]], [[Flujo de Registro y Sync Auth]]
