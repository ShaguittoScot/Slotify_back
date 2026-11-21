---
title: US-022 - Aislamiento Multi-Inquilino por Token JWT
aliases:
  - US-022
  - Aislamiento Multi-Inquilino
  - Seguridad JWT
tags:
  - historia-usuario
  - seguridad
  - multi-tenant
  - jwt
  - us-022
estado_documental: "Reconstruida desde implementación"
implementacion_backend: "Completa"
integracion_frontend: "Discrepante / Bloqueante"
---

# 🛡️ US-022: Aislamiento Multi-Inquilino por Token JWT

> [!warning] Reconstruida a partir del código. Pendiente de validación funcional.
> Esta historia representa un requerimiento transversal técnico y de seguridad no funcional ("Architecture/Security Story"). Se formaliza a partir de su constante referenciación en controladores (`AuthController`, `BusinessesController`, `CalendarController`, `ScheduleBlocksController`), la interfaz `ICurrentUserService`, la suite de pruebas `ProtectedEndpointsTests.cs` y los filtros de pertenencia de negocio.

* **Como:** Oficial de seguridad y arquitecto del sistema Slotify.
* **Quiero:** Que cada petición a endpoints protegidos valide criptográficamente un token JWT portador del claim obligatorio `businessId` y verifique la titularidad del recurso.
* **Para:** Garantizar el aislamiento estricto de datos entre negocios inquilinos (multi-tenancy) e impedir fugas o accesos cruzados no autorizados.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Invocación autorizada con pertenencia de negocio válida
```gherkin
Dado que un usuario realiza una petición a un endpoint protegido (ej. GET /api/calendar/slots o POST /api/schedule-blocks)
Cuando adjunta una cabecera Authorization: Bearer <token> con un JWT válido firmado por Slotify
Entonces el middleware JwtBearer valida la firma y vigencia del token
Y CurrentUserService extrae los claims userId y businessId
Y el controlador o handler filtra los datos exclusivamente para el businessId resuelto
Y la operación responde exitosamente con código HTTP 200/201.
```

### Escenario 2: Petición sin token o con token malformado
```gherkin
Dado que una petición omite la cabecera Authorization o incluye un token corrupto
Cuando llega al pipeline de ASP.NET Core
Entonces el middleware de autenticación rechaza la petición de forma inmediata
Y retorna código HTTP 401 Unauthorized sin invocar a los controladores.
```

### Escenario 3: Acceso a recurso de otro negocio (Intento de acceso cruzado)
```gherkin
Dado que un usuario autenticado con businessId = 1 intenta modificar un recurso perteneciente a businessId = 2
(por ejemplo PUT /api/businesses/2/template o DELETE /api/schedule-blocks/{id_de_otro_negocio})
Cuando el backend evalúa la correspondencia del identificador
Entonces se detecta la discrepancia de tenencia
Y el servidor rechaza la petición retornando código HTTP 403 Forbidden.
```

---

## ⚙️ Implementación Backend Real

* **Servicios de Identidad y Contexto:**
  - `ICurrentUserService` / `CurrentUserService` (resolución de `UserId` y `BusinessId` desde `HttpContext.User`)
  - `IJwtService` / `JwtService` (generación y firma de tokens)
* **Middleware HTTP:**
  - `AuthenticationMiddleware` (`JwtBearerDefaults.AuthenticationScheme`)
  - `AuthorizationMiddleware` (`[Authorize]`)
* **Endpoint de Emisión/Intercambio:**
  - `POST /api/auth/login` (`ExchangeTokenCommand` / `ExchangeTokenHandler` en [AuthController.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.API/Controllers/AuthController.cs))
* **Entidades Protegidas:** [[Entidad Business]], [[Entidad Appointment]], [[Entidad ScheduleBlock]], [[Entidad AvailabilitySchedule]], `BusinessBrand`
* **Tablas de Base de Datos Impactadas:** `"negocios"`, `"citas"`, `"bloqueos_agenda"`, `"horarios_disponibilidad"`, `"marca_negocio"`

---

## 🛡️ Aislamiento Interno vs. Seguridad de Identidad en el Perímetro

> [!important] Distinción Arquitectónica Fundamental
> Para un análisis riguroso de la seguridad, deben separarse claramente dos componentes:
> 
> 1. **Aislamiento Interno de Datos (Comprobado y Funcional):**
>    - En todos los controladores protegidos con `[Authorize]`, el sistema extrae el `businessId` del token mediante `CurrentUserService` e impide accesos cruzados entre negocios (retornando `403 Forbidden` ante discordancias de inquilino).
>    - Este comportamiento está plenamente cubierto y respaldado por las pruebas en `ProtectedEndpointsTests.cs`.
> 
> 2. **Seguridad de Emisión en el Perímetro (Vulnerabilidad Identificada Estáticamente):**
>    - El proceso por el cual un llamador obtiene dicho token en `POST /api/auth/login` carece de verificación criptográfica externa: acepta `{ userId, email }` sin validar la firma del token emitido por Supabase.
>    - Adicionalmente, el asistente de alta en `POST /api/onboarding/complete-business-onboarding` opera como endpoint anónimo sin comprobar tokens.
>    - **Conclusión:** Las pruebas unitarias demuestran que el aislamiento de datos opera correctamente sobre peticiones portadoras de tokens válidos, pero **no demuestran que el proceso de emisión inicial o el alta de onboarding sean invulnerables a suplantaciones de identidad.**

---

## 📱 Estado de Integración con el Frontend

* **Nivel de Integración:** Discrepante / Bloqueante.
* **Problema Identificado:**
  - El frontend (`src/shared/lib/api.ts`) almacena y envía el token emitido directamente por Supabase Auth en las cabeceras `Authorization: Bearer <token>`.
  - El backend valida tokens firmados simétricamente con su propio secreto interno (`Jwt:Secret`) y requiere el claim `businessId` para operar en endpoints protegidos.
  - Al recibir el token de Supabase, los endpoints protegidos del backend fallan con `401 Unauthorized` (firma no válida) o `403 Forbidden` (falta del claim `businessId`).
  - **Alineación pendiente:** El cliente debe intercambiar sus credenciales de sesión en `POST /api/auth/login` enviando `{ userId, email }`, recibir el JWT propio de Slotify con el claim `businessId` y adjuntarlo en sus peticiones a endpoints protegidos.

---

## 🧪 Pruebas que la Respaldan

* **Suite de Pruebas Automatizadas:** `Slotify.Tests/ProtectedEndpointsTests.cs`:
  - `GetCalendarSlots_SinToken_Retorna401Unauthorized`
  - `PostScheduleBlocks_SinToken_Retorna401Unauthorized`
  - `GetCalendarSlots_ConTokenExpirado_Retorna401Unauthorized`
  - `PutBookingConfig_ConTokenDeOtroNegocio_Retorna403Forbidden`
  - `PutTemplate_ConTokenDeOtroNegocio_Retorna403Forbidden`
  - `DeleteScheduleBlock_DeOtroNegocio_Retorna403Forbidden`
  - `PutBookingConfig_ConTokenDelMismoNegocio_Retorna200Ok`
  - `GetBookingConfigBySlug_SinToken_EsPublicoYRetorna200Ok`
  - `GetBusinesses_SinToken_EsPublicoYRetorna200Ok`
  - `PutBranding_ConTokenDeOtroNegocio_Retorna403Forbidden`
  - `GetCalendarSlots_ConTokenValido_FiltraUnicamentePorElNegocioDelToken`

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-000 - Autenticacion y Registro de Administrador]]
* [[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes]]
* [[Feature Auth]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Pipeline Behaviors]]
* [[Clean Architecture]]
