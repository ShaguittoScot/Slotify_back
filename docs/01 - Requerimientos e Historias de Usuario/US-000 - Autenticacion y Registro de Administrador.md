---
title: US-000 - Autenticacion y Registro de Administrador
aliases:
  - US-000
  - Autenticación y Registro de Administrador
tags:
  - historia-usuario
  - auth
  - admin
  - jwt
  - us-000
estado_documental: "Original verificada"
implementacion_backend: "Parcial / Discrepancia Crítica"
integracion_frontend: "Discrepante / Bloqueante"
---

# 🔑 US-000: Autenticación y Registro de Administrador

> [!warning] Discrepancia Crítica entre Requisito Original e Implementación Real
> Esta historia proviene de los requisitos originales de la plataforma (clasificación de procedencia: **Original verificada**). Sin embargo, la inspección técnica del código confirma que la arquitectura evolucionó hacia la delegación de credenciales en **Supabase Auth**.
> El backend **no implementa hashing local activo, registro nativo de contraseñas ni login por password**. El flujo real opera mediante sincronización de perfiles (`POST /api/auth/sync`) e intercambio de identificadores (`POST /api/auth/login`).

* **Como:** Dueño / Administrador de un negocio.
* **Quiero:** Registrarme como administrador mediante Supabase Auth e intercambiar mi sesión por un token JWT propio de Slotify con el claim `businessId`.
* **Para:** Acceder de manera exclusiva a las funciones administrativas y operativas de mi negocio bajo el esquema de aislamiento multi-tenant.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Sincronización de nuevo administrador tras alta en Supabase
```gherkin
Dado que un nuevo administrador se ha registrado previamente en Supabase Auth obteniendo un UUID (Id) y un correo electrónico
Cuando el cliente envía la solicitud POST /api/auth/sync con los datos del administrador y del negocio
Entonces el backend valida que el correo no exista en la tabla usuarios y el slug comercial esté disponible
Y persiste atómicamente el registro en la tabla usuarios con rol DUENO y password_hash = 'managed_by_supabase'
Y crea el negocio en la tabla negocios
Y retorna código HTTP 200 OK con AuthUserDto conteniendo los datos de perfil y un JWT inicial de Slotify.
```

### Escenario 2: Intercambio de sesión por JWT de Slotify (Login)
```gherkin
Dado que el administrador posee una cuenta activa registrada en el sistema
Cuando el cliente envía la solicitud POST /api/auth/login con { userId, email }
Entonces el backend busca al usuario en la tabla usuarios por identificador y correo
Y si existe y está activo, genera un token JWT firmado simétricamente con claims: sub (userId), email, role, businessId
Y retorna código HTTP 200 OK con AuthResponse conteniendo el token de acceso emitido.
```

### Escenario 3: Rechazo por credenciales inexistentes o discrepantes
```gherkin
Dado que se envía una solicitud POST /api/auth/login con un userId o email que no coinciden en la base de datos
Cuando el handler ejecuta la búsqueda
Entonces el servidor rechaza la solicitud retornando código HTTP 400 Bad Request
Y reporta el error de dominio correspondiente (DomainErrors.Auth.InvalidCredentials).
```

---

## ⚙️ Implementación Backend Real

* **Controlador:** [AuthController.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.API/Controllers/AuthController.cs)
* **Rutas HTTP Reales:**
  - `POST /api/auth/sync` (Sincronización de administrador: `RegisterAdminCommand` / `RegisterAdminHandler`)
  - `POST /api/auth/login` (Intercambio de token: `ExchangeTokenCommand` / `ExchangeTokenHandler`)
  - `POST /api/auth/sync-client` (Sincronización de cliente consumidor: `RegisterClientCommand` / `RegisterClientHandler`)
* **Estado de Endpoints Nativos Anteriores:**
  - `POST /api/auth/register-admin`: **Inexistente en el backend actual** (reemplazado por `/api/auth/sync`).
  - `LoginQuery` / `LoginHandler` con contraseña nativa: **Inexistente en el backend actual**.
* **Estado de `IPasswordHasher` y `PasswordHasher`:**
  - Registrado en el contenedor de dependencias (`DependencyInjection.cs:47`), pero **no participa en ningún flujo activo de autenticación**.
  - El campo `password_hash` en la tabla `usuarios` se persiste con la constante `"managed_by_supabase"`.
* **Servicios de Identidad:** `IJwtService` / `JwtService` (firma HMAC-SHA256 con `Jwt:Secret`).
* **Entidades Involucradas:** [[Entidad User]], [[Entidad Business]]
* **Tablas de Base de Datos Impactadas:** `"usuarios"`, `"negocios"`

---

## 🛡️ Riesgos de Seguridad Identificados (Inspección Estática)

> [!caution] Vulnerabilidad Crítica en Emisión de Identidad
> 1. **Endpoint Anónimo sin Firma Externa:** `POST /api/auth/login` está decorado con `[AllowAnonymous]` y acepta únicamente `{ userId, email }` en el cuerpo JSON. No recibe ni valida la firma criptográfica del token de Supabase ni realiza consultas a JWKS o a la API de Supabase.
> 2. **Riesgo Estático de Suplantación:** Cualquier llamador que conozca el UUID y correo electrónico de un administrador puede solicitar un token JWT válido con rol `DUENO` y el `businessId` correspondiente, obteniendo acceso a las funciones protegidas del inquilino sin demostrar posesión de credenciales activas.
> 3. **Nota Metodológica:** Este riesgo ha sido identificado mediante análisis e inspección estática del código fuente; no constituye evidencia de explotación o ataque activo en entornos de producción.

---

## 📱 Estado de Integración con el Frontend

* **Nivel de Integración:** Discrepante / Bloqueante.
* **Mecanismo Frontend:**
  - El cliente (`src/shared/lib/api.ts`) almacena y envía el token emitido directamente por Supabase Auth en `Authorization: Bearer <token>`.
  - El frontend no invoca actualmente `POST /api/auth/login` para realizar el intercambio por el JWT de Slotify.
  - Al recibir el token de Supabase en endpoints protegidos, el backend falla la validación de firma simétrica con `401 Unauthorized` o carece del claim `businessId` (`403 Forbidden`).
* **Expiración y Renovación:**
  - El JWT del backend tiene una vigencia criptográfica real de **15 minutos** (`JwtService.cs`), pero el DTO `AuthResponse` informa `ExpiresAt = DateTime.UtcNow.AddMinutes(60)`.
  - El backend no cuenta con endpoint ni lógica para refresco de JWT propio (`/auth/refresh` no implementado en backend).

---

## 🧪 Pruebas Automatizadas

* **Suite de Pruebas:** `Slotify.Tests/ProtectedEndpointsTests.cs` (valida aislamiento por token y rechazo sin token).
* **Pruebas de Repositorio:** `BusinessRepositoryTests.cs`.
* **Pruebas de Handler:** `ExchangeTokenHandlerTests` (integradas en suite de pruebas de solución).

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes]]
* [[US-022 - Aislamiento Multi-Inquilino por Token JWT]]
* [[Feature Auth]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad User]]
* [[Entidad Business]]
* [[Flujo de Registro y Sync Auth]]
