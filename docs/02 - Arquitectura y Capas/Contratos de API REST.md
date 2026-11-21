---
title: Contratos de API REST (Slotify.API)
tags:
  - api
  - rest
  - contratos
  - openapi
  - endpoints
  - dtos
---

# 📡 Contratos de API REST (`Slotify.API`)

Este documento constituye la **especificación técnica exhaustiva y formal** de todos los contratos HTTP RESTful implementados en Slotify Backend (.NET 9 / ASP.NET Core).

Su propósito es servir como contrato estricto de integración para el equipo de Frontend (React Native / Expo y Web) y garantizar que la comunicación cliente-servidor se realice sin ambigüedades de enrutamiento, parámetros, esquemas JSON o códigos de estado.

---

## 📋 1. Convenciones Globales y Normas de Consumo

### 1.1 Cabeceras Obligatorias (Headers)

| Cabecera | Valor | Requerida en | Descripción |
| :--- | :--- | :--- | :--- |
| `Content-Type` | `application/json` | Peticiones con cuerpo (`POST`, `PUT`, `PATCH`) | Especifica la serialización en formato JSON. |
| `Accept` | `application/json` | Todas las peticiones | Solicita la respuesta en formato JSON. |
| `Authorization` | `Bearer <token_jwt>` | Endpoints protegidos (`[Authorize]`) | Token JWT emitido por Slotify con el claim `businessId`. |

### 1.2 Formatos de Respuesta HTTP

El backend implementa **tres patrones de respuesta de éxito**:

1. **Envuelto en `Result<T>` (Estándar):**
   ```json
   {
     "success": true,
     "message": null,
     "errors": [],
     "data": { ... }
   }
   ```
2. **Colección Directa (Array Plano sin envoltorio):**
   Retornado por endpoints de catálogo público como `GET /api/businesses` y `GET /api/sector-templates`.
   ```json
   [
     { ... },
     { ... }
   ]
   ```
3. **Respuesta Directa u Objeto Anónimo:**
   - `GET /api/businesses/{slug}` retorna directamente el DTO `BusinessDetailDto` (US-023).
   - `GET /api/businesses/{slug}/booking-config` retorna directamente el DTO `BookingFormConfigDto`.
   - `PUT /api/businesses/{id}/template` retorna `{ "success": true }`.
   - `PUT /api/businesses/{id}/booking-config` retorna `{ "message": "..." }`.
   - `POST /api/calendar/appointments` retorna `{ "success": true, "id": "...", "data": "..." }`.

### 1.3 Formato Global de Error

Cuando una petición falla (por validación, reglas de negocio o errores internos), el backend devuelve una estructura estándar:

```json
{
  "success": false,
  "message": "Descripción clara del motivo del fallo.",
  "errors": [
    "Detalle específico de validación o causa del error."
  ],
  "error": "Primer error de la lista para consumo simplificado en UI."
}
```

Códigos de estado utilizados:
- `400 Bad Request`: Datos de entrada inválidos, reglas de validación fallidas (`ValidationException`) o parámetros ausentes.
- `401 Unauthorized`: Ausencia, firma no válida o expiración del token JWT Bearer.
- `403 Forbidden`: El token no pertenece al negocio solicitado o carece del claim `businessId`.
- `404 Not Found`: Recurso no encontrado (negocio, plantilla o bloqueo).
- `409 Conflict`: Conflicto de estado (ej. correo ya registrado).
- `500 Internal Server Error`: Excepción no controlada en el servidor.

---

## 🔐 2. Endpoints del Módulo de Autenticación (`/api/auth`)

Conecta con [[Feature Auth]], [[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes|US-001]] y [[Entidad User]].

### 2.1 `POST /api/auth/login`
- **Propósito:** Intercambia la sesión autenticada de Supabase Auth (`UserId` y `Email`) por un token JWT emitido por Slotify que incluye el claim de negocio (`businessId`).
- **Autenticación:** Pública (`[AllowAnonymous]`). No requiere cabeceras de autorización.
- **Handler / CQRS:** `ExchangeTokenHandler` ([ExchangeTokenCommand.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Auth/Commands/ExchangeTokenCommand.cs)).
- **Encabezados:** `Content-Type: application/json`.
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "admin@barberiacentral.com"
  }
  ```
  - `userId` (Guid, Obligatorio): Identificador UUID del usuario en Supabase.
  - `email` (string, Obligatorio): Correo electrónico registrado.
  > [!caution] Consideración Crítica de Seguridad (Inspección Estática)
  > El endpoint no recibe un token JWT de Supabase ni valida su firma criptográfica externa. Opera con base en la coincidencia de `userId` y `email` en la base de datos local. En consecuencia, un llamador que conozca ambos valores puede solicitar un JWT legítimo con el claim `businessId` y rol `DUENO`. Este hallazgo proviene de análisis estático de código; no hay constancia de explotación en producción.
- **Respuesta Exitosa (`200 OK` - Envuelto en `Result<AuthResponse>`):**
  ```json
  {
    "success": true,
    "message": null,
    "errors": [],
    "data": {
      "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "refreshToken": "4a7b8c9d0e1f...",
      "expiresAt": "2026-11-18T15:30:00Z",
      "user": {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "firstName": "Juan",
        "lastName": "Pérez",
        "fullName": "Juan Pérez",
        "email": "admin@barberiacentral.com",
        "role": "DUENO",
        "businessId": "7cb4a7c8-9df2-4ce1-8637-29cb1152a412",
        "accessToken": null,
        "refreshToken": null
      }
    }
  }
  ```
  > [!warning] Discrepancia Confirmada: Expiración del Token JWT
  > `AuthResponse.ExpiresAt` informa `DateTime.UtcNow.AddMinutes(60)` (60 minutos), pero el token emitido físicamente en `JwtService.cs` vence a los **15 minutos** (`Expires = DateTime.UtcNow.AddMinutes(15)`). Con `ClockSkew = TimeSpan.Zero`, cualquier petición posterior a 15 minutos falla con `401 Unauthorized`. No existe endpoint de refresh token propio en el backend.

- **Respuestas de Error:**
  - `400 Bad Request`: Si el usuario no existe en la base de datos o no tiene un negocio asignado (`"El usuario aún no cuenta con un negocio sincronizado."`).
- **Consumo Frontend:** Declarado en `constants.ts` como `API_ENDPOINTS.AUTH.LOGIN`, pero **actualmente no consumido por el frontend** (el frontend guarda el token directo de Supabase en storage mediante `storage.secureSet('accessToken', ...)`).

---

### 2.2 `POST /api/auth/sync`
- **Propósito:** Registra un nuevo administrador (`DUENO`), crea su negocio inicial en una transacción atómica y emite su primer JWT con el claim de negocio.
- **Autenticación:** Pública (`[AllowAnonymous]`).
- **Handler / CQRS:** `RegisterAdminHandler` ([RegisterAdminCommand.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Auth/Commands/RegisterAdminCommand.cs)).
- **Validador:** `RegisterAdminValidator` (FluentValidation).
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "firstName": "Juan",
    "lastName": "Pérez",
    "fullName": "Juan Pérez",
    "email": "admin@barberiacentral.com",
    "businessName": "Barbería Central",
    "businessPhone": "+5491122334455",
    "sectorTemplateId": 1
  }
  ```
  - `id` (Guid, Obligatorio): UUID emitido por Supabase Auth.
  - `firstName` (string, Obligatorio): Nombre de pila.
  - `lastName` (string, Obligatorio): Apellidos.
  - `fullName` (string, Obligatorio): Nombre completo concatenado.
  - `email` (string, Obligatorio, Formato Email): Correo electrónico único.
  - `businessName` (string, Obligatorio, Máx. 100 caracteres): Razón social o nombre comercial.
  - `businessPhone` (string, Obligatorio): Teléfono de contacto comercial.
  - `sectorTemplateId` (int, Obligatorio, > 0): ID de la plantilla de sector inicial elegida.
- **Respuesta Exitosa (`200 OK` - Envuelto en `Result<AuthUserDto>`):**
  ```json
  {
    "success": true,
    "message": null,
    "errors": [],
    "data": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "firstName": "Juan",
      "lastName": "Pérez",
      "fullName": "Juan Pérez",
      "email": "admin@barberiacentral.com",
      "role": "DUENO",
      "businessId": "7cb4a7c8-9df2-4ce1-8637-29cb1152a412",
      "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "refreshToken": "4a7b8c9d0e1f..."
    }
  }
  ```
- **Respuestas de Error:**
  - `400 Bad Request`: Parámetros inválidos según `RegisterAdminValidator`.
  - `409 Conflict`: Si el correo o el ID de usuario ya se encuentran registrados en el sistema.
- **Consumo Frontend:** Invocado por `businessOnboardingApi.ts` / `authApi.ts` -> `syncUser(data)`.

---

### 2.3 `POST /api/auth/sync-client`
- **Propósito:** Registra o sincroniza un cliente consumidor final (`CLIENTE`) en la tabla `clientes` del sistema.
- **Autenticación:** Pública (`[AllowAnonymous]`).
- **Handler / CQRS:** `RegisterClientHandler` ([RegisterClientCommand.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Auth/Commands/RegisterClientCommand.cs)).
- **Validador:** `RegisterClientValidator`.
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "id": "c1f8a847-195c-4876-882f-3b7c9339b123",
    "firstName": "Carlos",
    "lastName": "López",
    "fullName": "Carlos López",
    "email": "carlos.cliente@gmail.com",
    "phone": "+5491199887766"
  }
  ```
  - `id` (Guid, Obligatorio): UUID emitido por Supabase Auth para el cliente.
  - `firstName` (string, Obligatorio).
  - `lastName` (string, Obligatorio).
  - `fullName` (string, Obligatorio).
  - `email` (string, Obligatorio, Formato Email).
  - `phone` (string, Opcional): Teléfono del cliente.
- **Respuesta Exitosa (`200 OK` - Envuelto en `Result<ClientDto>`):**
  > [!note] Sin Token en Respuesta de Cliente
  > El cliente consumidor final se autentica mediante Supabase; `ClientDto` retorna la información de perfil sincronizada y **no genera tokens JWT de backend**.
  ```json
  {
    "success": true,
    "message": null,
    "errors": [],
    "data": {
      "id": "c1f8a847-195c-4876-882f-3b7c9339b123",
      "supabaseId": "c1f8a847-195c-4876-882f-3b7c9339b123",
      "firstName": "Carlos",
      "lastName": "López",
      "fullName": "Carlos López",
      "email": "carlos.cliente@gmail.com",
      "phone": "+5491199887766"
    }
  }
  ```
- **Respuestas de Error:**
  - `400 Bad Request`: Validación de campos obligatorios fallida o correo ya registrado con otro SupabaseId.
- **Consumo Frontend:** Invocado por `authApi.ts` -> `registerClient(request)`.

---

## 🚀 3. Endpoints del Módulo de Onboarding (`/api/onboarding`)

Conecta con [[Feature Onboarding]], [[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes|US-001]], [[Entidad BusinessBrand]] y [[Entidad Service]].

### 3.1 `POST /api/onboarding/complete-business-onboarding`
- **Propósito:** Transacción de alta comercial en 5 pasos: actualiza datos básicos del negocio, persiste los horarios de apertura semanales (`horarios_disponibilidad`), registra el catálogo inicial de servicios (`servicios`) y estampa la fecha de finalización (`onboarding_completed_at`).
- **Autenticación:** Protegida (`[Authorize]` en [OnboardingController.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.API/Controllers/OnboardingController.cs)). Exige token JWT de Slotify con claim de rol `Owner` y claim `businessId`. Resuelve la identidad exclusivamente vía `ICurrentUserService` (`ClaimTypes.NameIdentifier` / `sub`), neutralizando cualquier intento de suplantación de identidad o modificación cruzada multi-inquilino.
- **Handler / CQRS:** `CompleteBusinessOnboardingHandler` ([CompleteBusinessOnboardingCommand.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Onboarding/Commands/CompleteBusinessOnboardingCommand.cs)).
- **Validador:** `CompleteBusinessOnboardingValidator`.
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "firstName": "Juan",
    "lastName": "Pérez",
    "fullName": "Juan Pérez",
    "business": {
      "name": "Barbería Premium",
      "phone": "+5491122334455",
      "sectorTemplateId": 1,
      "timeZone": "America/Argentina/Buenos_Aires"
    },
    "businessHours": [
      {
        "dayOfWeek": 1,
        "startTime": "09:00",
        "endTime": "19:00"
      },
      {
        "dayOfWeek": 2,
        "startTime": "09:00",
        "endTime": "19:00"
      },
      {
        "dayOfWeek": 0,
        "startTime": "00:00",
        "endTime": "00:00"
      }
    ],
    "services": [
      {
        "name": "Corte Clásico",
        "durationMinutes": 30,
        "price": 15.00
      },
      {
        "name": "Arreglo de Barba",
        "durationMinutes": 20,
        "price": 10.00
      }
    ]
  }
  ```
  - `firstName` (string, Obligatorio, Máx. 100): Nombre del propietario.
  - `lastName` (string, Opcional, Máx. 100): Apellido del propietario.
  - `fullName` (string, Opcional): Nombre completo.
  - `business` (`OnboardingBusinessDto`, Obligatorio):
    - `name` (string, Obligatorio, Máx. 150): Nombre del negocio.
    - `phone` (string, Opcional, Máx. 20): Teléfono de contacto comercial.
    - `sectorTemplateId` (int, Obligatorio, > 0): ID de plantilla de sector elegida.
    - `timeZone` (string, Obligatorio): Identificador de zona horaria IANA.
  - `businessHours` (Array de `OnboardingBusinessHourDto`, Mínimo 1 elemento):
    - `dayOfWeek` (short, 0=Domingo a 6=Sábado).
    - `startTime` (TimeOnly, formato "HH:mm" o "HH:mm:ss").
    - `endTime` (TimeOnly, formato "HH:mm" o "HH:mm:ss", posterior a `startTime` salvo cerrado).
  - `services` (Array de `OnboardingServiceDto`, Mínimo 1 elemento):
    - `name` (string, Obligatorio): Nombre del servicio.
    - `durationMinutes` (int, Obligatorio, > 0): Duración del servicio.
    - `price` (decimal, Obligatorio, >= 0): Precio unitario pactado.
- **Respuesta Exitosa (`200 OK` - Envuelto en `Result<AuthUserDto>`):**
  ```json
  {
    "success": true,
    "message": null,
    "errors": [],
    "data": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "firstName": "Juan",
      "lastName": "Pérez",
      "fullName": "Juan Pérez",
      "email": "admin@barberiapremium.com",
      "role": "Owner",
      "businessId": "7cb4a7c8-9df2-4ce1-8637-29cb1152a412",
      "accessToken": null,
      "refreshToken": null
    }
  }
  ```
- **Respuestas de Error:**
  - `400 Bad Request`: Si faltan campos funcionales obligatorios o el negocio contiene datos parciales de onboarding.
  - `401 Unauthorized`: Si la petición carece de token JWT, tiene firma inválida o el token ha expirado.
  - `403 Forbidden`: Si el usuario autenticado no posee rol `Owner`, carece de claim `businessId` o su `businessId` del token no coincide con el asignado al usuario.
  - `404 Not Found`: Si el usuario o su negocio no existen en la base de datos.
  - `409 Conflict`: Si el nombre comercial genera un slug que ya está en uso por otro establecimiento.
- **Consumo Frontend:** Invocado por `businessOnboardingApi.ts` -> `completeBusinessOnboarding(payload)` con cabecera `Authorization: Bearer <slotify_jwt>`. Compatible con `CompleteBusinessOnboardingRequest`.

---

## 🏢 4. Endpoints del Módulo de Negocios (`/api/businesses`)

Conecta con [[Feature Businesses]], [[US-002 - Plantillas por Sector y Configuracion de Negocio|US-002]], [[Entidad Business]] y [[Entidad BusinessBrand]].

### 4.1 `GET /api/businesses`
- **Propósito:** Retorna el catálogo público de negocios activos (con onboarding completo) para el explorador del consumidor final.
- **Autenticación:** Pública (`[AllowAnonymous]`).
- **Handler / CQRS:** `GetBusinessesHandler` ([GetBusinessesQuery.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Businesses/Queries/GetBusinessesQuery.cs)).
- **Parámetros:** Ninguno.
- **Respuesta Exitosa (`200 OK` - Array Plano `BusinessListItemDto[]`):**
  > [!note] Formato de Respuesta
  > Este endpoint retorna un **arreglo directo** de objetos, sin envoltorio `Result<T>`.
  ```json
  [
    {
      "id": "7cb4a7c8-9df2-4ce1-8637-29cb1152a412",
      "name": "Barbería Central",
      "slug": "barberia-central",
      "phone": "+5491122334455",
      "sectorTemplateId": 1,
      "sectorName": "Barbería",
      "logoUrl": "https://cdn.slotify.app/logos/brand-1.png",
      "accentColor": "#1E3A8A"
    }
  ]
  ```
- **Consumo Frontend:** Invocado por `exploreApi.ts` -> `getBusinesses()`.

---

### 4.2 `PUT /api/businesses/{id}/template`
- **Propósito:** Modifica la plantilla de sector asignada a un negocio existente.
- **Autenticación:** Protegida (`[Authorize]`).
- **Aislamiento Multi-Inquilino:** Verifica que `{id}` coincida con el claim `businessId` del JWT. Si no coincide, deniega con `403 Forbidden`.
- **Parámetros de Ruta:**
  - `id` (Guid, Obligatorio): Identificador del negocio.
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "newTemplateId": 2
  }
  ```
- **Respuesta Exitosa (`200 OK` - Objeto Anónimo `{ success: true }`):**
  ```json
  {
    "success": true
  }
  ```
- **Respuestas de Error:**
  - `401 Unauthorized`: Token ausente o expirado.
  - `403 Forbidden`: Intento de modificar un negocio ajeno.
  - `404 NotFound`: Negocio o plantilla no encontrados.
- **Consumo Frontend:** `sectorTemplatesApi.ts` intenta erróneamente invocar `PATCH /businesses/${id}/sector` (Ruta inexistente).

---

### 4.3 `GET /api/businesses/{slug}/booking-config`
- **Propósito:** Obtiene la configuración pública del formulario dinámico de reservas junto con los colores corporativos y logotipo del negocio.
- **Autenticación:** Pública (`[AllowAnonymous]`).
- **Handler / CQRS:** `GetBusinessBookingConfigQueryHandler`.
- **Parámetros de Ruta:**
  - `slug` (string, Obligatorio): Slug único del negocio (ej. `"barberia-central"`).
- **Respuesta Exitosa (`200 OK` - DTO Directo `BookingFormConfigDto`):**
  ```json
  {
    "businessId": "7cb4a7c8-9df2-4ce1-8637-29cb1152a412",
    "businessName": "Barbería Central",
    "logoUrl": "https://cdn.slotify.app/logos/brand-1.png",
    "primaryColor": "#1E3A8A",
    "secondaryColor": "#3B82F6",
    "fields": [
      {
        "fieldId": "hair_type",
        "label": "Tipo de Cabello",
        "type": "select",
        "isRequired": true,
        "order": 1,
        "options": ["Lacio", "Ondulado", "Rizado"]
      }
    ]
  }
  ```
- **Respuestas de Error:**
  - `404 NotFound`: No existe ningún negocio con el slug especificado.
- **Consumo Frontend:** Invocado por `BookingScreen.tsx` -> `fetch('${API_URL}/businesses/${slug}/booking-config')`.

---

### 4.4 `PUT /api/businesses/{id}/booking-config`
- **Propósito:** Actualiza la configuración de campos del formulario dinámico de reservas en la columna `configuracion_formulario_personalizada` (JSONB).
- **Autenticación:** Protegida (`[Authorize]`).
- **Aislamiento Multi-Inquilino:** Requiere coincidencia de `{id}` con el `businessId` del token JWT.
- **Parámetros de Ruta:**
  - `id` (Guid, Obligatorio): Identificador del negocio.
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "formConfig": {
      "businessId": "7cb4a7c8-9df2-4ce1-8637-29cb1152a412",
      "businessName": "Barbería Central",
      "logoUrl": "https://cdn.slotify.app/logos/brand-1.png",
      "primaryColor": "#1E3A8A",
      "secondaryColor": "#3B82F6",
      "fields": []
    }
  }
  ```
- **Respuesta Exitosa (`200 OK` - Mensaje de Confirmación):**
  ```json
  {
    "message": "Configuración e identidad de marca actualizadas exitosamente."
  }
  ```
- **Consumo Frontend:** Invocado por `FormBuilderScreen.tsx`.

---

### 4.5 `PUT /api/businesses/{id}/branding` y `PATCH /api/businesses/{id}/branding`
- **Propósito:** Actualiza la identidad visual de marca del negocio (logotipo, colores corporativos y opcionalmente nombre comercial) en las tablas `marca_negocio` y `negocios`.
- **Autenticación:** Protegida (`[Authorize]`). Aislamiento por claim `businessId`.
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "logoUrl": "https://cdn.slotify.app/logos/nuevo-logo.png",
    "accentColor": "#0F172A",
    "secondaryColor": "#38BDF8",
    "businessName": "Barbería Central Deluxe"
  }
  ```
- **Respuesta Exitosa (`200 OK` - Envuelto en `Result<BookingFormConfigDto>`):**
  ```json
  {
    "success": true,
    "message": null,
    "errors": [],
    "data": {
      "businessId": "7cb4a7c8-9df2-4ce1-8637-29cb1152a412",
      "businessName": "Barbería Central Deluxe",
      "logoUrl": "https://cdn.slotify.app/logos/nuevo-logo.png",
      "primaryColor": "#0F172A",
      "secondaryColor": "#38BDF8",
      "fields": []
    }
  }
  ```
- **Consumo Frontend:** Compatible pero no consumida de forma directa (el frontend utiliza `PUT /booking-config`).

---

### 4.6 `GET /api/businesses/{slug}`
- **Propósito:** Retorna el detalle público y catálogo de servicios de un negocio activo mediante su slug único, para la pantalla de detalle y selección de servicios en el explorador de clientes B2C.
- **Autenticación:** Pública (`[AllowAnonymous]`).
- **Handler / CQRS:** `GetBusinessBySlugHandler` ([GetBusinessBySlugQuery.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Businesses/Queries/GetBusinessBySlugQuery.cs)).
- **Parámetros de Ruta:**
  - `slug` (string, Obligatorio): Slug amigable y único del negocio (ej. `"barberia-don-juan"`).
- **Regla de Negocio:** Solo retorna negocios con `OnboardingCompletedAt != null` y filtra servicios con `IsActive == true && DeletedAt == null`.
- **Respuesta Exitosa (`200 OK` - Objeto Plano `BusinessDetailDto`):**
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
- **Respuesta de Error (`404 Not Found`):**
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
- **Consumo Frontend:** Consumido directamente por `exploreApi.getBusinessBySlug(slug)` en `src/features/explore/api/index.ts`.
- **Historia de Usuario:** [[US-023 - Detalle Publico de Negocios por Slug|US-023]].

---

## 📅 5. Endpoints del Módulo de Calendario y Citas (`/api/calendar`)

Conecta con [[Feature Calendar]], [[US-003 - Calendario Tactil Multiformato y Consulta de Citas|US-003]] y [[Entidad Appointment]].

### 5.1 `GET /api/calendar/slots`
- **Propósito:** Retorna la vista unificada de disponibilidad del calendario para el negocio del token autenticado. Unifica citas confirmadas, bloqueos manuales y horarios de atención dentro de una lista polimórfica de slots.
- **Autenticación:** Protegida (`[Authorize]`). Extrae automáticamente `businessId` desde el JWT (`ICurrentUserService`).
- **Handler / CQRS:** `GetCalendarSlotsHandler` ([GetCalendarSlotsQuery.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Calendar/Queries/GetCalendarSlotsQuery.cs)).
- **Parámetros de Consulta (Query Params):**
  - `startDate` (DateTime ISO 8601, Obligatorio): Fecha/hora inicio del rango.
  - `endDate` (DateTime ISO 8601, Obligatorio): Fecha/hora fin del rango.
  - `employeeId` (Guid, Opcional): Filtro específico por colaborador/empleado.
- **Respuesta Exitosa (`200 OK` - Envuelto en `Result<CalendarResponse>`):**
  ```json
  {
    "success": true,
    "message": null,
    "errors": [],
    "data": {
      "rangeStart": "2026-11-20T00:00:00Z",
      "rangeEnd": "2026-11-20T23:59:59Z",
      "slots": [
        {
          "type": "appointment",
          "resourceId": "4da42b31-31a8-48be-88e5-3d077c5c0d11",
          "startTime": "2026-11-20T10:00:00Z",
          "endTime": "2026-11-20T10:45:00Z",
          "title": "Corte y Barba - Martín Gómez",
          "status": "confirmed",
          "clientName": "Martín Gómez",
          "clientPhone": "+5491133445566",
          "servicePrice": "$25.00",
          "employeeName": "Ariel",
          "blockReason": null
        },
        {
          "type": "block",
          "resourceId": "15",
          "startTime": "2026-11-20T13:00:00Z",
          "endTime": "2026-11-20T14:00:00Z",
          "title": "Almuerzo de equipo",
          "status": "blocked",
          "clientName": null,
          "clientPhone": null,
          "servicePrice": null,
          "employeeName": null,
          "blockReason": "Almuerzo de equipo"
        },
        {
          "type": "available",
          "resourceId": "slot-20261120-1400",
          "startTime": "2026-11-20T14:00:00Z",
          "endTime": "2026-11-20T14:30:00Z",
          "title": "Disponible",
          "status": "available",
          "clientName": null,
          "clientPhone": null,
          "servicePrice": null,
          "employeeName": null,
          "blockReason": null
        }
      ]
    }
  }
  ```

#### Estructura Polimórfica de `CalendarSlotDto`:
- **`type` (string):**
  - `"appointment"`: Cita registrada en la tabla `citas`. Utiliza `clientName`, `clientPhone`, `servicePrice` y `employeeName`.
  - `"block"`: Bloqueo manual registrado en `bloqueos_agenda`. Utiliza `blockReason`.
  - `"available"`: Franja horaria disponible calculada a partir de `horarios_disponibilidad`.
- **`status` (string):** `"confirmed"`, `"cancelled"`, `"blocked"`, `"available"`.
- **`rangeStart` y `rangeEnd` (DateTime):** Marcas de tiempo UTC del rango consultado.

- **Consumo Frontend:** Consumido por `calendar/api/index.ts` -> `fetchCalendarSlots(...)`. Coincide 100% con la interfaz `CalendarResponse` del frontend.

---

### 5.2 `POST /api/calendar/appointments`
- **Propósito:** Crea una nueva cita en el calendario del negocio autenticado.
- **Autenticación:** Protegida (`[Authorize]`). Extrae `businessId` desde el JWT.
- **Handler / CQRS:** `CreateAppointmentHandler` ([CreateAppointmentCommand.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Calendar/Commands/CreateAppointmentCommand.cs)).
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "employeeId": null,
    "startTime": "2026-11-20T11:00:00Z",
    "endTime": "2026-11-20T11:45:00Z",
    "clientName": "Alejandro Rossi",
    "clientEmail": "alejandro@empresa.com",
    "clientPhone": "+5491144556677",
    "agreedTotal": 25.00
  }
  ```
- **Respuesta Exitosa (`200 OK` - Objeto Anónimo con ID generado):**
  ```json
  {
    "success": true,
    "id": "9bc32d1e-84b2-4d56-a977-ef5121345f89",
    "data": "9bc32d1e-84b2-4d56-a977-ef5121345f89"
  }
  ```
- **Consumo Frontend:** Compatible pero no consumida (el frontend no cuenta con UI ni llamadas para invocar este endpoint).

---

## 🚫 6. Endpoints del Módulo de Bloqueos de Horario (`/api/schedule-blocks`)

Conecta con [[Feature ScheduleBlocks]], [[Bloqueos de Agenda]], [[US-008 - Bloqueo Manual de Horarios y Disponibilidad|US-008]] y [[Entidad ScheduleBlock]].

### 6.1 `POST /api/schedule-blocks`
- **Propósito:** Registra un nuevo bloqueo manual de horario en la agenda del negocio del token.
- **Autenticación:** Protegida (`[Authorize]`). Extrae `businessId` desde el JWT.
- **Cuerpo de Entrada (Request Body):**
  ```json
  {
    "employeeId": null,
    "startDateTime": "2026-11-20T14:00:00Z",
    "endDateTime": "2026-11-20T15:30:00Z",
    "reason": "Mantenimiento eléctrico en el local"
  }
  ```
- **Respuesta Exitosa (`201 Created` - Envuelto en `Result<long>` con cabecera `Location`):**
  - Cabecera: `Location: /api/schedule-blocks?id=15`
  - Cuerpo:
  ```json
  {
    "success": true,
    "message": null,
    "errors": [],
    "data": 15
  }
  ```
- **Consumo Frontend:** No consumida. `schedule-blocks/api/index.ts` mantiene un `TODO` y arroja `Error('US-008: createScheduleBlock no implementado')`.

---

### 6.2 `GET /api/schedule-blocks`
- **Propósito:** Retorna la lista de bloqueos manuales registrados en un rango temporal para el negocio autenticado.
- **Autenticación:** Protegida (`[Authorize]`).
- **Respuesta Exitosa (`200 OK` - Envuelto en `Result<IEnumerable<ScheduleBlockDto>>`):**
  ```json
  {
    "success": true,
    "message": null,
    "errors": [],
    "data": [
      {
        "id": 15,
        "employeeId": null,
        "startDateTime": "2026-11-20T14:00:00Z",
        "endDateTime": "2026-11-20T15:30:00Z",
        "reason": "Mantenimiento eléctrico en el local"
      }
    ]
  }
  ```

---

### 6.3 `DELETE /api/schedule-blocks/{id}`
- **Propósito:** Elimina un bloqueo manual de agenda (desbloquea el horario) garantizando la tenencia del negocio.
- **Autenticación:** Protegida (`[Authorize]`). Extrae `businessId` desde el JWT.
- **Respuesta Exitosa (`200 OK` - Envuelto en `Result`):**
  ```json
  {
    "success": true,
    "message": null,
    "errors": []
  }
  ```
- **Consumo Frontend:** No consumida (`schedule-blocks/api/index.ts` arroja `Error`).

---

## 🎨 7. Endpoints del Módulo de Plantillas de Sector (`/api/sector-templates`)

Conecta con [[Feature SectorTemplates]], [[US-002 - Plantillas por Sector y Configuracion de Negocio|US-002]] y [[Entidad SectorTemplate]].

### 7.1 `GET /api/sector-templates`
- **Propósito:** Retorna el catálogo completo de plantillas de rubro comercial (seed data) con módulos y servicios sugeridos.
- **Autenticación:** Pública (`[AllowAnonymous]`).
- **Respuesta Exitosa (`200 OK` - Array Plano `SectorTemplateDto[]`):**
  ```json
  [
    {
      "id": 1,
      "name": "Barbería",
      "description": "Ideal para barberías y peluquerías masculinas con gestión rápida de turnos.",
      "icon": "scissors",
      "defaultModules": [
        "citas",
        "servicios",
        "empleados"
      ],
      "suggestedServices": [
        {
          "name": "Corte de Cabello",
          "durationMinutes": 30,
          "suggestedPrice": 15.00
        }
      ]
    }
  ]
  ```
- **Consumo Frontend:** Consumido en `sector-templates/api/index.ts` -> `fetchSectorTemplates()`.

---

## 🩺 8. Endpoints de Infraestructura y Diagnóstico

### 8.1 `GET /health`
- **Propósito:** Verificación de salud y liveness del contenedor/proceso backend.
- **Implementación:** Minimal API en [Program.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.API/Program.cs#L113).
- **Autenticación:** Pública (`[AllowAnonymous]`).
- **Respuesta Exitosa (`200 OK`):**
  ```json
  {
    "status": "Healthy",
    "timestamp": "2026-11-18T14:22:10.5124021Z"
  }
  ```

---

## ⚠️ 9. Matriz de Integración con Frontend e Incidencias Confirmadas

| # | Área | Endpoint / Archivo | Estado | Evidencia y Riesgo Técnico | Recomendación |
| :---: | :--- | :--- | :---: | :--- | :--- |
| **1** | **Autenticación** | `POST /api/auth/login`<br>`api.ts` | **Incompatible / No consumida** | El frontend almacena el token emitido por Supabase en `storage.secureSet('accessToken')`. No llama a `/api/auth/login`. Al enviar ese token al backend, la firma simétrica falla o no contiene el claim `businessId`. | El frontend debe implementar la llamada a `POST /api/auth/login` tras autenticarse en Supabase para obtener el JWT propio de Slotify. |
| **2** | **Seguridad Login** | `AuthController.cs` / `ExchangeTokenCommand` | **Vulnerabilidad Crítica (Inspección Estática)** | El backend no valida criptográficamente el token Supabase; solo recibe `userId` y `email` en un endpoint `[AllowAnonymous]`. Un usuario que conozca ambos datos podría obtener un JWT de administrador con claim `businessId`. | Añadir validación de la firma del token Supabase mediante JWKS o API en `ExchangeTokenHandler`. |
| **3** | **Seguridad Onboarding** | `OnboardingController.cs` / `CompleteBusinessOnboardingCommand` | **Remediado (Fase 10.3)** | El endpoint fue protegido con `[Authorize]`, inyecta `ICurrentUserService`, valida los claims `sub`, `role == "Owner"` y `businessId`, e ignora cualquier dato de identidad del body. Cubierto con 12 pruebas de seguridad en `OnboardingSecurityTests.cs`. | Remediación completada en backend. |
| **4** | **Expiración JWT** | `JwtService.cs` vs `AuthResponse.cs` | **Discrepancia Confirmada** | El token JWT físico expira a los 15 minutos (`AddMinutes(15)`), pero `ExpiresAt` reporta 60 minutos. Con `ClockSkew = TimeSpan.Zero`, las peticiones fallan con `401` a los 15 min sin refresco preventivo. | Homogeneizar la expiración en 60 minutos o sincronizar `ExpiresAt` con la expiración real. |
| **5** | **Explorador Negocios** | `GET /businesses/${slug}` | **Remediado (US-023)** | Endpoint implementado con `[AllowAnonymous]`, devuelve el DTO plano `BusinessDetailDto`, filtra por `OnboardingCompletedAt != null` y servicios activos. Totalmente compatible con `exploreApi.getBusinessBySlug`. | Remediación completada en backend. |
| **6** | **Giro Comercial** | `PATCH /businesses/${id}/sector` | **Ruta Inexistente** | `sectorTemplatesApi.ts` invoca `PATCH /businesses/${id}/sector`. El backend expone `PUT /api/businesses/{id}/template`. Retorna `404 Not Found`. | Corregir la ruta y método en el frontend hacia `PUT /api/businesses/${id}/template`. |
| **7** | **Bloqueos de Agenda** | `POST`/`DELETE /api/schedule-blocks` | **No consumida** | El backend tiene endpoints completos y probados, pero `schedule-blocks/api/index.ts` mantiene stubs que lanzan excepciones. | Conectar la implementación de `schedule-blocks/api/index.ts` con `apiClient`. |
| **8** | **Creación de Citas** | `POST /api/calendar/appointments` | **No consumida** | El backend tiene implementado el endpoint de agendamiento, pero ninguna pantalla o servicio del frontend lo consume. | Integrar el formulario de agendamiento del frontend con este endpoint. |
| **9** | **Parseo de Errores** | `errorUtils.ts` | **Incompatible** | El frontend espera `errors` como un mapa `Record<string, string[]>`. El backend devuelve un arreglo plano `errors: string[]`. | Actualizar `errorUtils.ts` para soportar `string[]`. |

---

## 🔗 Relaciones con la Documentación de la Bóveda

- **Capa y Middleware:** [[Modulo API]]
- **Casos de Uso y Handlers:** [[Modulo Application]]
- **Entidades de Dominio:** [[Entidad User]], [[Entidad Client]], [[Entidad Business]], [[Entidad BusinessBrand]], [[Entidad Service]], [[Entidad Appointment]], [[Entidad ScheduleBlock]], [[Entidad SectorTemplate]]
- **Módulos Funcionales (Features):** [[Feature Auth]], [[Feature Onboarding]], [[Feature Businesses]], [[Feature Calendar]], [[Feature ScheduleBlocks]], [[Feature SectorTemplates]]
- **Historias de Usuario (Scrum):** [[Indice de Historias de Usuario]]
- **Índice General:** [[Indice General]]
