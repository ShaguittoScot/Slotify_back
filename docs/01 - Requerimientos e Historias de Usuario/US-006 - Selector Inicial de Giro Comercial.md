---
title: US-006 - Selector Inicial de Giro Comercial
aliases:
  - US-006
  - Selector de Giro Comercial
tags:
  - historia-usuario
  - giro-comercial
  - plantillas
  - onboarding
  - us-006
estado_documental: "Original verificada"
implementacion_backend: "Completa"
integracion_frontend: "Integrada"
---

# 🏢 US-006: Selector Inicial de Giro Comercial

* **Como:** Dueño de negocio en proceso de alta/onboarding en Slotify.
* **Quiero:** Visualizar e interactuar con una cuadrícula visual de categorías y giros comerciales (Barbería, Salón de Belleza, Spa, Consultorio Médico/Dental, etc.).
* **Para:** Seleccionar el rubro de mi negocio y precargar la configuración visual y operativa sugerida para mi sector.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Presentación de giros comerciales en el asistente de registro
```gherkin
Dado que el usuario inicia el wizard de onboarding B2B
Cuando accede al paso de selección de giro comercial
Entonces la app solicita GET /api/sector-templates al backend
Y el backend retorna el catálogo activo de plantillas de sector con sus módulos y servicios recomendados
Y la interfaz muestra tarjetas interactivas con iconos, nombres de sector y descripción.
```

### Escenario 2: Selección y confirmación de sector
```gherkin
Dado que el usuario navega por las opciones de plantillas
Cuando selecciona una categoría (ej. "Barbería") y presiona Continuar
Entonces el identificador sectorTemplateId queda seleccionado en useSectorTemplateStore
Y se envía como parámetro opcional en el comando de alta del negocio (RegisterAdminCommand o CompleteBusinessOnboardingCommand)
Y el negocio queda preconfigurado con el giro y servicios del sector elegido.
```

---

## ⚙️ Implementación Backend Real

* **Controlador:** `SectorTemplatesController.cs`
* **Ruta HTTP:** `GET /api/sector-templates`
* **Consulta MediatR:** `GetSectorTemplatesQuery` / `GetSectorTemplatesHandler`
* **Entidad Involucrada:** [[Entidad SectorTemplate]]
* **Tabla de Base de Datos Impactada:** `plantillas_sector`

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `SectorSelectionScreen.tsx`, `SectorTemplateCard.tsx`, `SectorTemplateSelector.tsx`, `templates.tsx`
* **Módulo Frontend:** `src/features/sector-templates`
* **Store:** `useSectorTemplateStore`
* **Nivel de Integración:** Integrada.
* **Detalle:** El frontend consume el endpoint `GET /api/sector-templates` y mantiene en caché los giros canónicos para el asistente de configuración.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas de Repositorio:** `SectorTemplateRepositoryTests.cs`.
* **Pruebas de Endpoint:** `ProtectedEndpointsTests.cs`.

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-002 - Plantillas por Sector y Configuracion de Negocio]]
* [[US-013 - Cambio de Plantilla en Negocio Existente]]
* [[Feature SectorTemplates]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad SectorTemplate]]
