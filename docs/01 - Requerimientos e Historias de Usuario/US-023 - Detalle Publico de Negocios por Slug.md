---
title: US-023 - Detalle Público de Negocios por Slug
aliases:
  - US-023
  - Detalle Público por Slug
  - Perfil Público del Negocio
tags:
  - historia-usuario
  - negocios
  - catalogo
  - servicios
  - b2c
  - explorador
  - us-023
estado_documental: "Formalizada e Implementada"
implementacion_backend: "Completa"
integracion_frontend: "Integrada"
---

# 🌐 US-023: Detalle Público de Negocios por Slug

> [!abstract] Resumen de la Historia
> Permite a cualquier usuario o cliente consumidor (B2C) consultar toda la información comercial pública y el catálogo de servicios vigentes de un negocio utilizando su `slug` amigable (URL-friendly), sirviendo como pantalla previa al agendamiento de citas en la aplicación web o móvil.

* **Como:** Cliente consumidor o usuario anónimo que explora servicios en Slotify.
* **Quiero:** Consultar el perfil público, datos comerciales y catálogo de servicios activos de un establecimiento a través de su `slug` (ej. `barberia-don-juan`).
* **Para:** Conocer los precios, duraciones, tiempos de preparación y seleccionar los servicios que deseo agendar.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Consulta exitosa de negocio con onboarding finalizado
```gherkin
Dado que existe un negocio con slug "barberia-don-juan" en la base de datos
Y dicho negocio ha finalizado su onboarding comercial ("OnboardingCompletedAt" no es nulo)
Cuando un cliente envía una petición GET /api/businesses/barberia-don-juan sin cabecera Authorization
Entonces el backend responde con código HTTP 200 OK
Y el cuerpo de la respuesta contiene el objeto JSON plano con la información pública del negocio:
  | id, name, slug, phone, category, categoryKey, timeZone, minAdvanceHours, maxAdvanceDays, logoUrl, brandColor, services |
Y la lista de servicios incluye únicamente los servicios con "IsActive" = true y "DeletedAt" = null.
```

### Escenario 2: Negocio inexistente o con slug inválido
```gherkin
Dado que un cliente solicita un slug que no está registrado en el sistema
Cuando envía una petición GET /api/businesses/slug-inexistente
Entonces el backend responde con código HTTP 404 Not Found
Y devuelve una respuesta JSON estandarizada con "success" = false y el mensaje de error correspondiente.
```

### Escenario 3: Negocio registrado pero con onboarding comercial incompleto
```gherkin
Dado que un negocio existe en la base de datos pero su "OnboardingCompletedAt" es nulo (onboarding B2B pendiente)
Cuando un cliente intenta consultar su perfil público mediante GET /api/businesses/{slug}
Entonces el backend deniega la visualización y responde con código HTTP 404 Not Found
Y no revela datos privados ni configuración parcial del negocio.
```

---

## 🛡️ Reglas de Negocio y Seguridad

1. **Regla de Visibilidad Pública:**
   * La consulta requiere obligatoriamente que `Business.OnboardingCompletedAt != null`. Negocios en proceso de registro o abandonados no son visibles para el público.
2. **Acceso Anónimo (`[AllowAnonymous]`):**
   * El endpoint no exige token de autenticación JWT. Cualquier cliente final puede explorarlo.
3. **Catálogo de Servicios Depurado:**
   * Se excluyen servicios dados de baja (`DeletedAt != null`) o desactivados manualmente por el administrador (`IsActive == false`).
4. **Protección de Datos Privados:**
   * No se exponen correos, contraseñas, métricas financieras ni datos de empleados o propietarios.
5. **Fallback de Identidad Visual:**
   * Si el negocio no cuenta con registro en `BusinessBrand`, se utilizan los valores de `CustomFormConfig` (`logoUrl`, `accentColor`) de forma transparente.

---

## ⚙️ Implementación Backend

* **Controlador:** [[Modulo API|BusinessesController.cs]] (`src/Slotify.API/Controllers/BusinessesController.cs`)
* **Ruta HTTP:** `GET /api/businesses/{slug}`
* **Autorización:** `[AllowAnonymous]`
* **Patrón Arquitectónico:** Clean Architecture + CQRS + MediatR
* **Query & Handler:**
  * `GetBusinessBySlugQuery` (`src/Slotify.Application/Features/Businesses/Queries/GetBusinessBySlugQuery.cs`)
  * `GetBusinessBySlugHandler` (`src/Slotify.Application/Features/Businesses/Queries/GetBusinessBySlugHandler.cs`)
* **DTO de Salida:** `BusinessDetailDto` / `BusinessDetailServiceDto` (`src/Slotify.Application/Features/Businesses/DTOs/BusinessDetailDto.cs`)
* **Repositorio:** `IBusinessRepository.GetBySlugWithDetailsAsync` (`src/Slotify.Infrastructure/Data/Repositories/BusinessRepository.cs`)
* **Entidades Relacionadas:** [[Entidad Business]], [[Entidad BusinessBrand]], [[Entidad Service]], [[Entidad SectorTemplate]]

---

## 📡 Contrato de Respuesta

### Éxito (`200 OK` — Objeto DTO Plano):
```json
{
  "id": "7f8c1d2e-3b4a-4c5d-8e9f-0a1b2c3d4e5f",
  "name": "Barbería Don Juan",
  "slug": "barberia-don-juan",
  "phone": "+52 55 1234 5678",
  "category": "Barbería y Estilismo",
  "categoryKey": "barberia-y-estilismo",
  "timeZone": "America/Mexico_City",
  "minAdvanceHours": 2,
  "maxAdvanceDays": 30,
  "sectorTemplateId": 1,
  "logoUrl": "https://cdn.slotify.app/logos/don-juan.png",
  "brandColor": "#1E293B",
  "services": [
    {
      "id": "b1c2d3e4-f5a6-7b8c-9d0e-1f2a3b4c5d6e",
      "name": "Corte Clásico",
      "description": null,
      "price": 250.00,
      "durationMinutes": 45,
      "bufferTimeMinutes": 10,
      "isActive": true
    }
  ]
}
```

### Error (`404 Not Found`):
```json
{
  "success": false,
  "message": "Negocio no encontrado o no disponible públicamente.",
  "errors": [
    "Negocio no encontrado o no disponible públicamente."
  ],
  "error": "Negocio no encontrado o no disponible públicamente."
}
```

---

## 🧪 Pruebas Relacionadas

* **Integración HTTP (Pipeline real sin token y validación de 404):**
  * `ProtectedEndpointsTests.GetBusinessBySlug_SinToken_EsPublicoYRetorna200Ok`
  * `ProtectedEndpointsTests.GetBusinessBySlug_SlugInexistente_Retorna404NotFound`
* **Unitarias de Aplicación (Handler y Mapeo):**
  * `GetBusinessBySlugTests.Handle_SlugInvalidoOVacio_RetornaNotFound`
  * `GetBusinessBySlugTests.Handle_NegocioNoEncontradoORepositorioRetornaNull_RetornaNotFound`
  * `GetBusinessBySlugTests.Handle_NegocioExistente_MapeaCamposYServiciosCorrectamente`
  * `GetBusinessBySlugTests.Handle_FallbackDeLogoYColorDesdeCustomFormConfig_CuandoBrandNoLosDefine`
* **Unitarias de Infraestructura (EF Core In-Memory):**
  * `BusinessRepositoryTests.GetBySlugWithDetailsAsync_NegocioExistenteConOnboardingCompleto_RetornaNegocioConDetalles`
  * `BusinessRepositoryTests.GetBySlugWithDetailsAsync_NegocioSinOnboardingCompleto_RetornaNull`
  * `BusinessRepositoryTests.GetBySlugWithDetailsAsync_SlugInexistente_RetornaNull`
  * `BusinessRepositoryTests.GetBySlugWithDetailsAsync_FiltraServiciosInactivosYEliminados`
  * `BusinessRepositoryTests.GetBySlugWithDetailsAsync_UsaAsNoTracking_NoRastreaEntidadesEnChangeTracker`
  * `BusinessRepositoryTests.GetBySlugWithDetailsAsync_RespetaCancellationToken`

---

## 🔗 Relaciones con la Arquitectura

* [[Feature Businesses]]
* [[Contratos de API REST]]
* [[Indice de Historias de Usuario]]
* [[US-002 - Plantillas por Sector y Configuracion de Negocio]]
* [[US-017 - Onboarding Transaccional B2B y Formulario Dinamico]]
* [[Entidad Business]]
* [[Entidad Service]]
