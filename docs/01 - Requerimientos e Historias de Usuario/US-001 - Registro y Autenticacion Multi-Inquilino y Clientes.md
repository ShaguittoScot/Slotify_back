---
title: US-001 - Registro y Autenticacion Multi-Inquilino y Clientes
aliases:
  - US-001
  - Registro y Autenticación
tags:
  - historia-usuario
  - auth
  - multi-tenant
  - supabase
  - us-001
estado_documental: "Original verificada"
implementacion_backend: "Completa"
integracion_frontend: "Parcial / Discrepante"
---

# 🔐 US-001: Registro y Autenticación Multi-Inquilino y Clientes

* **Como:** Dueño de negocio o cliente consumidor final.
* **Quiero:** Registrar mi cuenta e iniciar sesión de forma segura, diferenciando el rol operativo (`DUENO` con negocio multi-tenant vs `CLIENTE` consumidor global), o ingresar con modo de demostración.
* **Para:** Administrar mi negocio o agendar citas en la plataforma con total protección de datos y aislamiento de inquilinos.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Registro de Dueño de Negocio (B2B Multi-Tenant)
```gherkin
Dado que un nuevo usuario se registra como Administrador/Dueño
Cuando envía su correo, contraseña y nombre de negocio
Entonces el sistema crea el usuario en Supabase Auth con rol DUENO
Y despacha POST /api/auth/sync hacia el backend con su Supabase User ID
Y el backend crea de forma atómica el registro en usuarios y la entidad en negocios
Y posteriormente mediante POST /api/auth/login o POST /api/auth/exchange-token adquiere el Token JWT propio de Slotify con el claim businessId.
```

### Escenario 2: Registro de Cliente Consumidor (B2C)
```gherkin
Dado que un cliente se registra desde la app móvil
Cuando completa su nombre, correo, teléfono y contraseña
Entonces se crea el usuario en Supabase Auth con rol CLIENTE
Y se sincroniza vía POST /api/auth/sync-client en la tabla clientes
Y se redirige inmediatamente al Micro-Onboarding de bienvenida.
```

### Escenario 3: Inicio de Sesión Modo Demo
```gherkin
Dado que el usuario ingresa demo@slotify.com o admin@slotify.com
Cuando presiona Iniciar Sesión
Entonces el frontend autentica localmente sin llamadas a la BD de producción
Y carga el catálogo simulado de citas y estadísticas offline.
```

---

## ⚙️ Implementación Backend Real

* **Controlador:** `AuthController.cs`
* **Rutas HTTP:**
  - `POST /api/auth/register-admin` (Registro nativo directo sin Supabase)
  - `POST /api/auth/login` (Login nativo con emisión de JWT Slotify)
  - `POST /api/auth/sync` (Sincronización de dueño proveniente de Supabase Auth)
  - `POST /api/auth/sync-client` (Sincronización de cliente B2C proveniente de Supabase Auth)
  - `POST /api/auth/exchange-token` (Intercambio de Supabase Bearer token por Slotify JWT con claim `businessId`)
* **Comandos / Handlers:**
  - `RegisterAdminCommand` / `RegisterAdminHandler`
  - `LoginQuery` / `LoginHandler`
  - `SyncUserCommand` / `SyncUserHandler`
  - `RegisterClientCommand` / `RegisterClientHandler`
  - `ExchangeTokenCommand` / `ExchangeTokenHandler`
* **Entidades Involucradas:** [[Entidad User]], [[Entidad Business]], [[Entidad Client]]
* **Tablas de Base de Datos Impactadas:** `usuarios`, `negocios`, `clientes`

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `LoginScreen.tsx`, `RegisterScreen.tsx`, `RegisterClientScreen.tsx`, `useAuthStore`
* **Nivel de Integración:** Parcial / Discrepante.
* **Discrepancia Crítica:**
  - El frontend autentica contra Supabase Auth y luego envía el Bearer token de Supabase a los endpoints del backend.
  - El backend requiere el token JWT nativo de Slotify firmado con su clave simétrica HMAC-SHA256 (`JwtService`), el cual contiene los claims obligatorios `userId`, `businessId` y `role`.
  - Para operar en modo multi-tenant seguro sin rechazos HTTP 401/403, el frontend debe invocar `POST /api/auth/exchange-token` tras autenticar en Supabase para obtener el JWT de Slotify y utilizarlo en las peticiones subsecuentes.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas de Integración:** `ProtectedEndpointsTests.cs` (validación de claims y rechazo sin token).
* **Pruebas Unitarias:** Validadores FluentValidation (`RegisterAdminValidator`, `RegisterClientValidator`, `LoginValidator`).

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[Feature Auth]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Flujo de Registro y Sync Auth]]
* [[Flujo de Registro y Sync Cliente Consumidor]]
* [[Entidad User]]
* [[Entidad Business]]
* [[Entidad Client]]
