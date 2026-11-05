---
title: US-005 - Micro-Onboarding para Consumidor Final
tags:
  - historia-usuario
  - cliente
  - onboarding
  - b2c
---

# 🚀 US-005: Micro-Onboarding para Consumidor Final (B2C)

* **Como:** Cliente consumidor recién registrado en Slotify.
* **Quiero:** Visualizar una introducción interactiva de 3 diapositivas sobre los beneficios de la app y seleccionar mis categorías de interés.
* **Para:** Aprender a reservar en segundos, activar recordatorios y personalizar mi explorador de negocios favoritos sin fricción.

---

## 🎯 Criterios de Aceptación (Gherkin)

`gherkin
Escenario: Flujo de bienvenida tras registro de cliente
  Dado que el cliente completa el formulario de registro
  Cuando se confirma la creación de la cuenta
  Entonces la app redirige a /(onboarding)/consumer
  Y presenta 3 slides visuales:
    1. Descubrimiento Local (Negocios cercanos y calificaciones)
    2. Reserva Inmediata 24/7 (Sin llamadas ni esperas)
    3. Recordatorios y Control Total (Avisos automáticos)
  Y un paso interactivo para elegir categorías favoritas (Barbería, Spa, Salud, etc.)
  Y permite en todo momento presionar Omitir para ir directo al explorador.
`

---

## 🔗 Componentes Asociados
* **Backend:** [[AuthController]] (POST /api/auth/sync-client), RegisterClientHandler.cs
* **Frontend:** ConsumerOnboardingScreen.tsx, RegisterClientScreen.tsx, (onboarding)/consumer.tsx
* **Flujos:** [[Flujo de Registro y Sync Cliente Consumidor]]
