---
title: US-002 - Plantillas por Sector y Configuracion de Negocio
tags:
  - historia-usuario
  - plantillas
  - onboarding
  - sectores
---

# 🏢 US-002 / US-006: Plantillas por Sector y Configuración de Negocio

* **Como:** Administrador / Dueño de un nuevo negocio en Slotify.
* **Quiero:** Seleccionar mi sector comercial (Barbería, Estética/Salón, Spa, Consultorio Dental, etc.) durante el onboarding inicial.
* **Para:** Obtener una preconfiguración instantánea de servicios sugeridos, duraciones, precios base y horarios de atención sin tener que empezar desde cero.

---

## 🎯 Criterios de Aceptación (Gherkin)

`gherkin
Escenario: Consulta y selección de plantilla comercial
  Dado que el administrador está en el Wizard de Onboarding
  Cuando se monta la pantalla de selección de giro
  Entonces el sistema solicita GET /api/sector-templates
  Y presenta las plantillas disponibles con sus módulos y servicios por defecto
  Cuando el usuario selecciona una plantilla y confirma
  Entonces el backend asocia id_plantilla_sector al negocio e inserta los servicios correspondientes en la base de datos.
`

---

## 🔗 Componentes Asociados
* **Backend:** SectorTemplatesController.cs, GetSectorTemplatesQueryHandler.cs, [[Entidad SectorTemplate]], ISectorTemplateRepository
* **Frontend:** 	emplates.tsx, sector-selection.tsx, useSectorTemplateStore
