---
title: Feature SectorTemplates (Plantillas por Rubro)
tags:
  - feature
  - rubros
  - plantillas
---

# 📋 Feature SectorTemplates

Ubicación en el código: `src/Slotify.Application/Features/SectorTemplates/`

Provee el catálogo de plantillas preconfiguradas para diferentes tipos de negocios (Barberías, Salones de Belleza, Consultorios, etc.), con configuraciones iniciales de duración de turnos y campos personalizados.

---

## 📂 Estructura de Archivos
* **Queries:**
  * `GetSectorTemplatesQuery.cs`: Obtiene todas las plantillas activas.
* **Handlers:**
  * `GetSectorTemplatesHandler.cs`: Recupera la lista de plantillas desde la BD.

---

## 🔗 Relaciones en el Grafo
- **Invocado por:** [[Modulo API]] (`SectorTemplatesController`)
- **Entidad:** [[Entidad SectorTemplate]]
- **Usado durante:** [[Flujo de Registro y Sync Auth]] al crear un [[Entidad Business]].
