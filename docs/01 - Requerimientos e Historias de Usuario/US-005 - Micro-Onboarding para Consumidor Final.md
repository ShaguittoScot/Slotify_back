---
title: US-005 - Micro-Onboarding para Consumidor Final
aliases:
  - US-005
  - Micro-Onboarding Consumidor
tags:
  - historia-usuario
  - cliente
  - onboarding
  - b2c
  - us-005
estado_documental: "Original verificada"
implementacion_backend: "Completa"
integracion_frontend: "Integrada"
---

# 🚀 US-005: Micro-Onboarding para Consumidor Final (B2C)

* **Como:** Cliente consumidor recién registrado en Slotify.
* **Quiero:** Visualizar una introducción interactiva de 3 diapositivas sobre los beneficios de la app y seleccionar mis categorías de interés.
* **Para:** Aprender a reservar en segundos, activar recordatorios y personalizar mi explorador de negocios favoritos sin fricción.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Flujo de bienvenida tras registro de cliente consumidor
```gherkin
Dado que el cliente completa el formulario de registro en la app móvil
Cuando se confirma la creación de la cuenta en Supabase Auth
Entonces el sistema despacha POST /api/auth/sync-client hacia el backend con su authId, email, name y phone
Y el backend persiste o actualiza al cliente en la tabla clientes
Y la app redirige a /(onboarding)/consumer presentando 3 slides visuales:
  1. Descubrimiento Local (Negocios cercanos y calificaciones)
  2. Reserva Inmediata 24/7 (Sin llamadas ni esperas)
  3. Recordatorios y Control Total (Avisos automáticos)
Y un paso interactivo para elegir categorías favoritas (Barbería, Spa, Salud, etc.)
Y permite en todo momento presionar Omitir para ir directo al explorador de comercios.
```

---

## ⚙️ Implementación Backend Real

* **Controlador:** `AuthController.cs`
* **Ruta HTTP:** `POST /api/auth/sync-client`
* **Comando MediatR:** `RegisterClientCommand` / `RegisterClientHandler`
* **Validador:** `RegisterClientValidator`
* **Entidad Involucrada:** [[Entidad Client]]
* **Tabla de Base de Datos Impactada:** `clientes`

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `ConsumerOnboardingScreen.tsx`, `RegisterClientScreen.tsx`, `(onboarding)/consumer.tsx`
* **Nivel de Integración:** Integrada.
* **Detalle de Integración:**
  - El frontend interactúa correctamente con Supabase Auth y sincroniza al cliente enviando la carga esperada a `POST /api/auth/sync-client`.
  - El flujo visual de diapositivas y selección de tags es puramente de experiencia en cliente (UI/UX) y no requiere llamadas complejas de backend adicionales.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas Unitarias:** `RegisterClientValidatorTests` (validación de campos obligatorios: email, name, authId).
* **Pruebas de Repositorio:** `ClientRepositoryTests.cs`.

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-001 - Registro y Autenticacion Multi-Inquilino y Clientes]]
* [[Feature Auth]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad Client]]
* [[Flujo de Registro y Sync Cliente Consumidor]]
