---
title: US-002 - Plantillas por Sector y Configuracion de Negocio
aliases:
  - US-002
  - Plantillas por Sector
tags:
  - historia-usuario
  - plantillas
  - onboarding
  - sectores
  - us-002
estado_documental: "Original verificada"
implementacion_backend: "Completa"
integracion_frontend: "Parcial / Discrepante"
---

# 🏢 US-002: Plantillas por Sector y Configuración de Negocio

* **Como:** Administrador / Dueño de un negocio en Slotify.
* **Quiero:** Consultar el catálogo de plantillas por sector comercial (Barbería, Estética/Salón, Spa, Consultorio Dental, etc.) y aplicarlas a la configuración de mi negocio.
* **Para:** Obtener una preconfiguración instantánea de servicios sugeridos, duraciones predeterminadas, precios base y configuración del formulario sin tener que empezar desde cero.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Consulta del catálogo de plantillas comerciales
```gherkin
Dado que el administrador está autenticado en la plataforma o completando el onboarding
Cuando consulta el listado de plantillas disponibles
Entonces el frontend envía GET /api/sector-templates
Y el backend retorna el listado de plantillas activas desde la tabla plantillas_sector
Y cada plantilla incluye su id, nombre, sector, configuración de formulario por defecto y lista de servicios sugeridos.
```

### Escenario 2: Aplicación de plantilla al registrar o configurar negocio
```gherkin
Dado que el administrador selecciona una plantilla por sector
Cuando confirma su elección durante el alta o la configuración
Entonces el backend asocia id_plantilla_sector al registro en la tabla negocios
Y siembra automáticamente los servicios sugeridos en la tabla servicios vinculados al id_negocio.
```

---

## ⚙️ Implementación Backend Real

* **Controladores:**
  - `SectorTemplatesController.cs`: `GET /api/sector-templates`
  - `BusinessesController.cs`: `PUT /api/businesses/{id}/template` (vinculado también con [[US-013 - Cambio de Plantilla en Negocio Existente]])
* **Comandos y Consultas:**
  - `GetSectorTemplatesQuery` / `GetSectorTemplatesHandler`
  - `ChangeBusinessTemplateCommand` / `ChangeBusinessTemplateHandler`
* **Entidades Involucradas:** [[Entidad SectorTemplate]], [[Entidad Business]], [[Entidad Service]]
* **Tablas de Base de Datos Impactadas:** `plantillas_sector`, `negocios`, `servicios`

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `templates.tsx`, `SectorSelectionScreen.tsx`, `useSectorTemplateStore`
* **Nivel de Integración:** Parcial / Discrepante.
* **Detalle de Integración y Discrepancias:**
  - La consulta `GET /api/sector-templates` está integrada correctamente para poblar las opciones del selector de giros.
  - Para cambiar o actualizar la plantilla de un negocio existente, el frontend invoca `PATCH /businesses/{id}/sector` enviando `{ sector: string }`, mientras que el contrato real del backend implementa `PUT /api/businesses/{id}/template` esperando `{ sectorTemplateId: number }`.
  - Nota de delimitación: El asistente inicial de selección en el onboarding B2B corresponde formalmente a [[US-006 - Selector Inicial de Giro Comercial]], mientras que la migración o cambio en un negocio existente corresponde a [[US-013 - Cambio de Plantilla en Negocio Existente]].

---

## 🧪 Pruebas que la Respaldan

* **Pruebas de Repositorio:** `SectorTemplateRepositoryTests.cs`.
* **Pruebas de Integración:** `ProtectedEndpointsTests.cs` (validación de acceso multi-tenant al modificar negocios).

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-006 - Selector Inicial de Giro Comercial]]
* [[US-013 - Cambio de Plantilla en Negocio Existente]]
* [[Feature SectorTemplates]]
* [[Feature Businesses]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad SectorTemplate]]
* [[Entidad Business]]
* [[Entidad Service]]
* [[Flujo General de Peticion]]
