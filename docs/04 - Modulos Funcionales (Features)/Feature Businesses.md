---
title: Feature Businesses (Gestión y Catálogo de Negocios)
tags:
  - feature
  - businesses
  - catalogo
  - templates
  - dynamic-forms
---

# 🏢 Feature Businesses

Ubicación en el código: `src/Slotify.Application/Features/Businesses/`  
Controlador: `src/Slotify.API/Controllers/BusinessesController.cs`

Gestiona el catálogo público de negocios para exploración de clientes, el cambio dinámico de plantillas de sector (US-013) y la configuración de esquemas JSON para formularios dinámicos de reserva (US-019).

---

## 📂 Estructura de Archivos

### 1. Commands (Escritura)
* **`ChangeBusinessTemplateCommand.cs` (US-013):**
  - Permite al administrador actualizar el giro comercial (`SectorTemplateId`) del negocio.
* **`UpdateBusinessFormConfigCommand.cs` (US-019):**
  - Guarda la configuración y esquema JSON de preguntas personalizadas para el proceso de agendamiento.

### 2. Queries (Lectura)
* **`GetBusinessBookingConfigQuery.cs` (US-019):**
  - Obtiene la configuración de reserva de un negocio dado su `slug` (o nombre), incluyendo duración base, política de anticipación (`minAdvanceHours`, `maxAdvanceDays`) y esquema de formulario de preguntas.

### 3. Endpoints Expuestos (`BusinessesController.cs`)
* `GET /api/businesses`: Listado de negocios con información de categoría y sector para el explorador móvil/web.
* `PUT /api/businesses/{id}/template`: Actualización de plantilla de sector.
* `GET /api/businesses/{slug}/booking-config`: Obtención de esquema y configuración de reserva.
* `PUT /api/businesses/{id}/booking-config`: Actualización de configuración JSON de campos de reserva.

---

## 🔗 Relaciones en el Grafo
- **Invocado por:** [[Modulo API]] (`BusinessesController`)
- **Entidades:** [[Entidad Business]], [[Entidad SectorTemplate]]
- **Flujos:** [[Flujo General de Peticion]], [[Flujo de Registro y Sync Auth]]
