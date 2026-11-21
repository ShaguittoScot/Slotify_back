---
title: US-017 - Onboarding Transaccional B2B y Formulario Dinamico
aliases:
  - US-017
  - Onboarding Transaccional B2B
  - Formularios Dinámicos
tags:
  - historia-usuario
  - onboarding
  - form-builder
  - branding
  - us-017
estado_documental: "Reconstruida desde implementación"
implementacion_backend: "Completa"
integracion_frontend: "Parcial / Discrepante"
---

# 🚀 US-017: Onboarding Transaccional B2B y Formulario Dinámico

> [!warning] Reconstruida a partir del código. Pendiente de validación funcional.
> Esta historia de usuario no existía como ficha documental aislada en el backend original. Se reconstruye a partir de la implementación de `CompleteBusinessOnboardingHandler`, la migración segura en `Program.cs` ("Safe auto-migration for US-017"), los endpoints de configuración de formulario dinámico y marca en `BusinessesController.cs`, y su correlación con la historia UX del frontend `US-020 (Form Builder)`.

* **Como:** Dueño de negocio que completa el proceso de alta en Slotify.
* **Quiero:** Finalizar el asistente de incorporación en una única transacción atómica (asociando giro, horarios de operación, identidad visual y campos del formulario de reserva).
* **Para:** Lanzar la operación de mi establecimiento con todos sus parámetros sincronizados y permitir que mis clientes completen formularios adaptados a mi sector comercial.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Finalización transaccional del onboarding B2B con identidad autenticada
```gherkin
Dado que el dueño de negocio cuenta con una sesión autenticada válida (token JWT con claims de rol Owner y businessId)
Y completó todos los pasos del wizard
Cuando envía POST /api/onboarding/complete-business-onboarding con el payload funcional consolidado
Entonces el middleware JwtBearer valida el token del usuario
Y CurrentUserService extrae los claims userId y businessId
Y el backend ejecuta una transacción atómica con IBusinessOnboardingPersistence
Y asocia la plantilla de sector en la tabla negocios del usuario autenticado
Y persiste los días y franjas de atención en la tabla horarios_disponibilidad
Y registra el logotipo predeterminado en la tabla marca_negocio
Y responde con código HTTP 200 OK confirmando el alta integral.
```

### Escenario 2: Actualización de configuración del formulario de reserva
```gherkin
Dado que el administrador accede a la sección de configuración de reserva
Cuando modifica los campos requeridos (ej. teléfono obligatorio, notas opcionales)
Y envía PUT /api/businesses/{id}/booking-config con el esquema JSON
Entonces el backend valida que el {id} pertenezca al claim businessId del usuario (403 si difiere)
Y actualiza el campo de configuración en la tabla negocios
Y retorna código HTTP 200 OK.
```

---

## ⚙️ Implementación Backend Real

* **Controladores:**
  - `OnboardingController.cs`: `POST /api/onboarding/complete-business-onboarding` (`[Authorize]`, extracción de identidad vía `ICurrentUserService`).
  - `BusinessesController.cs`: `PUT /api/businesses/{id}/booking-config`, `PUT /api/businesses/{id}/branding`
* **Comandos y Handlers:**
  - `CompleteBusinessOnboardingCommand` / `CompleteBusinessOnboardingHandler`
  - `UpdateBusinessFormConfigCommand` / `UpdateBusinessFormConfigHandler`
  - `UpdateBusinessBrandingCommand` / `UpdateBusinessBrandingHandler`
* **Entidades Involucradas:** [[Entidad Business]], `BusinessBrand`, [[Entidad AvailabilitySchedule]]
* **Tablas de Base de Datos Impactadas:** `negocios`, `marca_negocio`, `horarios_disponibilidad`

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `FormBuilderScreen.tsx`, `FormViewer.tsx`, `(onboarding)` screens
* **Correlación Frontend:** Corresponde conceptualmente a `US-020 (Creador Dinámico de Formularios)` en la documentación frontend.
* **Nivel de Integración:** Parcial / Discrepante.
* **Detalle:**
  - El frontend cuenta con un constructor visual de formularios, pero en varios flujos de onboarding persiste configuraciones mediante mocks o endpoints separados en lugar de concentrar la transacción final en `POST /api/onboarding/complete-business-onboarding`.
  - El esquema JSON de campos de reserva debe alinearse con la estructura de `BookingConfig` validada en el backend.
  - El frontend debe adjuntar el token JWT interno de Slotify en la cabecera `Authorization: Bearer <token>`.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas de Seguridad y Aislamiento:** `OnboardingSecurityTests.cs` (12 pruebas de integración HTTP y unitarias que validan rechazo anónimo, token expirado, roles, aislamiento multi-inquilino e idempotencia).
* **Pruebas de Integración:** `ProtectedEndpointsTests.cs` (validación de aislamiento multi-tenant en actualización de booking config y branding).
* **Pruebas Unitarias:** Validadores de comandos de onboarding.

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-002 - Plantillas por Sector y Configuracion de Negocio]]
* [[US-006 - Selector Inicial de Giro Comercial]]
* [[US-022 - Aislamiento Multi-Inquilino por Token JWT]]
* [[Feature Onboarding]]
* [[Feature Businesses]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad Business]]
* [[Entidad AvailabilitySchedule]]
