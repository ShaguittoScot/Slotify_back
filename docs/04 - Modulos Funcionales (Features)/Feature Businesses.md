

---
title: Feature Businesses (Gestión, Catálogo y Branding)
aliases:
  - Feature Businesses
  - Businesses Feature
  - Negocios
tags:
  - feature
  - businesses
  - catalogo
  - branding
  - form-config
  - templates
---

# 🏢 Feature Businesses (Catálogo, Configuración de Marca y Formularios)

> [!abstract] Propósito del Módulo
> Gestiona la identidad comercial, catálogo público de negocios para exploración de clientes (B2C), cambio de giro comercial (US-013), personalización de logotipos/colores corporativos (US-019) y esquemas JSON para formularios dinámicos de reserva.

---

## 🧭 Estado de Implementación

| Endpoint / Operación | Implementado | Probado | Integrado en Frontend | Estado Global |
| :--- | :---: | :---: | :---: | :---: |
| `GET /api/businesses` (Catálogo Público) | ✅ Sí | ✅ Sí (`GetBusinessesTests`) | ✅ Sí (`exploreApi.getBusinesses`) | **Implementado, Probado e Integrado** |
| `GET /api/businesses/{slug}` (Detalle Público) | ✅ Sí | ✅ Sí (`GetBusinessBySlugTests`, `ProtectedEndpointsTests`) | ✅ Sí (`exploreApi.getBusinessBySlug`) | **Implementado, Probado e Integrado (US-023)** |
| `PUT /api/businesses/{id}/template` (Cambio de Giro) | ✅ Sí | ✅ Sí (`ProtectedEndpointsTests`) | ✅ Sí (`templates.tsx`) | **Implementado, Probado e Integrado** |
| `GET /api/businesses/{slug}/booking-config` | ✅ Sí | ✅ Sí (`ProtectedEndpointsTests`) | ✅ Sí (`form-preview.tsx`, Booking) | **Implementado, Probado e Integrado** |
| `PUT /api/businesses/{id}/booking-config` | ✅ Sí | ✅ Sí (`ProtectedEndpointsTests`) | ✅ Sí (`Dynamic Form Builder`) | **Implementado, Probado e Integrado** |
| `PUT /api/businesses/{id}/branding` (Identidad) | ✅ Sí | ✅ Sí (`BusinessBrandingTests`) | ✅ Sí (`OnboardingStore` / Wizard) | **Implementado, Probado e Integrado** |

---

## 🛠️ Controlador y Rutas Asociadas

* **Controlador:** `src/Slotify.API/Controllers/BusinessesController.cs`
* **Ruta base:** `/api/businesses`
* **Seguridad:** Requiere `[Authorize]` a nivel clase, con exclusión explícita `[AllowAnonymous]` en endpoints de consulta pública.

| Método | Ruta | Autorización | Descripción | Códigos HTTP |
| :--- | :--- | :---: | :--- | :---: |
| `GET` | `/api/businesses` | `AllowAnonymous` | Lista pública de negocios activos con logotipo y servicios para el explorador. | `200` |
| `GET` | `/api/businesses/{slug}` | `AllowAnonymous` | Detalle integral y catálogo de servicios de un negocio activo mediante su slug (US-023). | `200`, `404` |
| `PUT` | `/api/businesses/{id}/template` | `Authorize` | Cambia la plantilla de sector del negocio (US-013). | `200`, `401`, `403`, `404` |
| `GET` | `/api/businesses/{slug}/booking-config` | `AllowAnonymous` | Obtiene el formulario dinámico y colores del negocio por su slug. | `200`, `404` |
| `PUT` | `/api/businesses/{id}/booking-config` | `Authorize` | Actualiza el JSON del formulario dinámico de reservas. | `200`, `400`, `401`, `403`, `404` |
| `PUT/PATCH` | `/api/businesses/{id}/branding` | `Authorize` | Carga logotipo y colores corporativos primario/secundario. | `200`, `400`, `401`, `403`, `404` |

---

## ⚙️ Commands, Queries, Handlers y Validaciones

```mermaid
graph TD
    Client["📱 Frontend"] --> BizCtrl["BusinessesController (/api/businesses)"]

    BizCtrl -->|GET /| QryAll["GetBusinessesQuery"]
    BizCtrl -->|GET /{slug}| QrySlug["GetBusinessBySlugQuery (US-023)"]
    BizCtrl -->|PUT /{id}/template| CmdTpl["ChangeBusinessTemplateCommand"]
    BizCtrl -->|GET /{slug}/booking-config| QryCfg["GetBusinessBookingConfigQuery"]
    BizCtrl -->|PUT /{id}/booking-config| CmdCfg["UpdateBusinessFormConfigCommand"]
    BizCtrl -->|PUT /{id}/branding| CmdBrn["UpdateBusinessBrandIdentityCommand"]

    CmdCfg --> ValCfg["🛡️ UpdateBusinessFormConfigValidator"]
    CmdBrn --> ValBrn["🛡️ UpdateBusinessBrandIdentityValidator"]

    QryAll --> HdlAll["GetBusinessesHandler"]
    QrySlug --> HdlSlug["GetBusinessBySlugHandler"]
    CmdTpl --> HdlTpl["ChangeBusinessTemplateCommandHandler"]
    QryCfg --> HdlCfg["GetBusinessBookingConfigQueryHandler"]
    CmdCfg --> HdlCfgCmd["UpdateBusinessFormConfigCommandHandler"]
    CmdBrn --> HdlBrn["UpdateBusinessBrandIdentityCommandHandler"]
```

### 1. `GetBusinessesQuery` (Catálogo Público)
* **Handler:** `GetBusinessesHandler.cs`
* **Lógica:** Consulta a través de `IBusinessRepository.GetAllWithDetailsAsync()` incluyendo la plantilla de sector, servicios activos (con `DeletedAt == null`) y registro de marca. Mapea hacia `BusinessListItemDto`.

### 2. `ChangeBusinessTemplateCommand` (US-013)
* **Handler:** `ChangeBusinessTemplateCommandHandler.cs`
* **Lógica:** Valida que el `BusinessId` coincida con el token autenticado. Actualiza `SectorTemplateId` en `negocios` y guarda mediante `IUnitOfWork`.

### 3. `GetBusinessBookingConfigQuery` (US-019)
* **Handler:** `GetBusinessBookingConfigQueryHandler.cs`
* **Lógica:** Busca el negocio por `slug`. Si posee `CustomFormConfig`, deserializa el JSON; de lo contrario, extrae la configuración por defecto de su [[Entidad SectorTemplate]].

### 4. `UpdateBusinessBrandIdentityCommand` (US-019)
* **Handler:** `UpdateBusinessBrandIdentityCommandHandler.cs`
* **Validador:** `UpdateBusinessBrandIdentityValidator.cs` (`FluentValidation`):
  - `AccentColor`: Obligatorio, valida formato hexadecimal `#RGB` o `#RRGGBB` vía `Business.IsValidCorporateHexColor`.
  - `SecondaryColor`: Opcional, valida formato hexadecimal.
  - `LogoUrl`: Valida que sea URL absoluta o Data URI Base64 < 4MB vía `Business.IsValidLogoSource`.
  - `BusinessName`: Máximo 150 caracteres.
* **Persistencia:** Actualiza tanto la entidad [[Entidad BusinessBrand]] (`marca_negocio`) como el bloque incrustado en el JSON de `CustomFormConfig` para consistencia inmediata en vistas públicas.

### 5. `GetBusinessBySlugQuery` (US-023)
* **Handler:** `GetBusinessBySlugHandler.cs`
* **Lógica:** Valida el slug, consulta con `IBusinessRepository.GetBySlugWithDetailsAsync()`, exige `OnboardingCompletedAt != null` (retorna `404 Not Found` en caso contrario), mapea los datos corporativos, genera la clave URL `categoryKey`, extrae el catálogo de servicios activos (`IsActive == true && DeletedAt == null`) y devuelve directamente el DTO plano `BusinessDetailDto`.

---

## 📦 Modelos y DTOs

### Entrada
* **`ChangeTemplateRequest`:** `{ int newTemplateId }`
* **`UpdateFormConfigRequest`:** `{ BookingFormConfigDto formConfig }`
* **`UpdateBusinessBrandingRequest`:**
  ```json
  {
    "logoUrl": "https://cdn.slotify.app/logos/barberia-capital.png",
    "accentColor": "#E07A5F",
    "secondaryColor": "#3D405B",
    "businessName": "Barbería Capital CDMX"
  }
  ```

### Salida
* **`BusinessListItemDto`:**
  ```json
  {
    "id": "b8f2d1e0-3c4a-4a5e-b9f1-7c8d9e0f1a2b",
    "name": "Barbería Capital CDMX",
    "slug": "barberia-capital-cdmx",
    "phone": "+525512345678",
    "category": "Barbería / Peluquería",
    "categoryKey": "barbershop",
    "timeZone": "America/Mexico_City",
    "minAdvanceHours": 2,
    "maxAdvanceDays": 30,
    "sectorTemplateId": 1,
    "logoUrl": "https://cdn.slotify.app/logos/barberia.png",
    "brandColor": "#E07A5F",
    "services": [
      {
        "id": "d1c2b3a4-5678-90ab-cdef-1234567890ab",
        "name": "Corte de Cabello",
        "price": 250.00,
        "durationMinutes": 45
      }
    ]
  }
  ```
* **`BusinessDetailDto` (US-023):** `{ id, name, slug, phone, category, categoryKey, timeZone, minAdvanceHours, maxAdvanceDays, sectorTemplateId, logoUrl, brandColor, services: [ { id, name, description, price, durationMinutes, bufferTimeMinutes, isActive } ] }`
* **`BookingFormConfigDto`:** `{ title, description, presentationStyle, accentColor, secondaryColor, logoUrl, businessName, fields }`

---

## 🗄️ Entidades y Tablas Involucradas
* [[Entidad Business]] ➡️ Tabla `"negocios"`
* [[Entidad BusinessBrand]] ➡️ Tabla `"marca_negocio"`
* [[Entidad Service]] ➡️ Tabla `"servicios"`
* [[Entidad SectorTemplate]] ➡️ Tabla `"plantillas_sector"`
* Persistencia administrada con [[AppDbContext]], [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]], `IBusinessRepository` y `ISectorTemplateRepository`.

---

## 🛡️ Seguridad y Aislamiento por Negocio (US-022)
* En todos los endpoints de modificación (`/template`, `/booking-config`, `/branding`), se valida que el parámetro de ruta `{id}` sea idéntico al claim `businessId` extraído del JWT mediante `ICurrentUserService`.
* Si un usuario autenticado intenta modificar el negocio de otro inquilino, la API retorna inmediatamente `403 Forbidden` (`ErrorType.Forbidden`).
* En endpoints públicos (`GET /api/businesses` y `GET /api/businesses/{slug}`), se exige `[AllowAnonymous]` y la regla de negocio `OnboardingCompletedAt != null` para prevenir la divulgación de negocios no operativos.

---

## 🧪 Pruebas Automatizadas Existentes
* **`GetBusinessesTests.cs` (Unitarias):**
  - Retorno de colección vacía si no hay negocios.
  - Mapeo completo de negocio con plantilla, marca y filtrado estricto de servicios activos (ignora lógicamente eliminados).
* **`GetBusinessBySlugTests.cs` (Unitarias de Aplicación - US-023):**
  - Validación de slug vacío o nulo (404).
  - Comprobación de negocio no encontrado o onboarding pendiente (404).
  - Mapeo completo de campos, servicios activos, fallbacks de branding y cálculo de `categoryKey`.
* **`BusinessBrandingTests.cs` (Unitarias):**
  - Validación y persistencia de logotipo y paleta cromática en marca y formulario.
* **`BusinessRepositoryTests.cs` (Integración de Datos):**
  - Pruebas de consulta general y por slug con inclusión de entidades relacionadas (`GetBySlugWithDetailsAsync`).
  - Verificación de comportamiento inmutable con `AsNoTracking()` y respeto a `CancellationToken`.
* **`ProtectedEndpointsTests.cs` (Integración HTTP):**
  - Confirma que `GET /api/businesses/{slug}` y `GET /api/businesses/{slug}/booking-config` responden `200 OK` plano sin requerir token JWT.
  - Confirma que consultar un slug inexistente retorna `404 Not Found`.
  - Confirma que alterar branding de un negocio ajeno devuelve `403 Forbidden`.

---

## ⚠️ Discrepancias y Limitaciones Detectadas
1. **Ruta `GET /businesses/{slug}` Remediada (US-023):** El endpoint fue implementado en `BusinessesController.cs` devolviendo directamente el DTO plano `BusinessDetailDto`, garantizando compatibilidad inmediata con `exploreApi.getBusinessBySlug`.
2. **Propiedades no persistidas en base de datos:** El frontend define `address`, `rating` y `reviewCount` en su modelo `BusinessDto`. El backend no posee dichas columnas en la tabla PostgreSQL `negocios` ni sistema de reseñas, por lo que se mantienen como opcionales en el frontend sin generar conflictos de contrato.

---

## 🔗 Trazabilidad con la Bóveda
* **Historias de Usuario:** [[US-002 - Plantillas por Sector y Configuracion de Negocio]], [[US-023 - Detalle Publico de Negocios por Slug|US-023]], [[Indice de Historias de Usuario]] (US-013, US-017, US-019, US-022)
* **Entidades:** [[Entidad Business]], [[Entidad BusinessBrand]], [[Entidad Service]], [[Entidad SectorTemplate]]
* **Capa Arquitectura:** [[Modulo API]], [[Modulo Application]], [[Pipeline Behaviors]]
* **Persistencia:** [[AppDbContext]], [[Repositorios]], [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]]
* **Flujos:** [[Flujo General de Peticion]]
