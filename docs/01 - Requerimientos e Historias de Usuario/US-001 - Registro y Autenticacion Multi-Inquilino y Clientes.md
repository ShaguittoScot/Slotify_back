---
title: US-001 - Registro y Autenticacion Multi-Inquilino y Clientes
tags:
  - historia-usuario
  - auth
  - multi-tenant
  - supabase
---

# 🔐 US-001: Registro y Autenticación Multi-Inquilino y Clientes

* **Como:** Dueño de negocio o cliente consumidor final.
* **Quiero:** Registrar mi cuenta e iniciar sesión de forma segura, diferenciando el rol operativo (DUENO con negocio multi-tenant vs CLIENTE consumidor global), o ingresar con modo de demostración.
* **Para:** Administrar mi negocio o agendar citas en la plataforma con total protección de datos.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Registro de Dueño de Negocio (B2B Multi-Tenant)
`gherkin
Dado que un nuevo usuario se registra como Administrador/Dueño
Cuando envía su correo, contraseña y nombre de negocio
Entonces el sistema crea el usuario en Supabase Auth con rol DUENO
Y despacha POST /api/auth/sync hacia el backend
Y el backend crea de forma atómica el registro en Users y la entidad en Businesses
Y retorna el Token JWT con el claim business_id.
`

### Escenario 2: Registro de Cliente Consumidor (B2C)
`gherkin
Dado que un cliente se registra desde la app móvil
Cuando completa su nombre, correo, teléfono y contraseña
Entonces se crea el usuario en Supabase Auth con rol CLIENTE
Y se sincroniza vía POST /api/auth/sync-client en la tabla clientes
Y se redirige inmediatamente al Micro-Onboarding de bienvenida.
`

### Escenario 3: Inicio de Sesión Modo Demo
`gherkin
Dado que el usuario ingresa demo@slotify.com o admin@slotify.com
Cuando presiona Iniciar Sesión
Entonces el frontend autentica localmente sin llamadas a la BD de producción
Y carga el catálogo simulado de citas y estadísticas offline.
`

---

## 🔗 Componentes Asociados
* **Backend:** [[AuthController]], RegisterAdminHandler.cs, RegisterClientHandler.cs, IClientRepository, [[Entidad User]], [[Entidad Business]]
* **Frontend:** RegisterScreen.tsx, RegisterClientScreen.tsx, useAuthStore
* **Flujos:** [[Flujo de Registro y Sync Auth]], [[Flujo de Registro y Sync Cliente Consumidor]]
