---
title: US-004 - Dashboard Dinamico y Manejo de Estados Vacios
tags:
  - historia-usuario
  - dashboard
  - metricas
  - estado-vacio
---

# 📊 US-004: Dashboard Dinámico y Manejo de Estados Vacíos

* **Como:** Administrador del negocio.
* **Quiero:** Ver un resumen operativo con métricas clave en tiempo real (*Citas Hoy, Clientes, Ingresos Hoy, Pendientes*) y la lista de próximas citas.
* **Para:** Evaluar el rendimiento diario de mi negocio y acceder rápidamente a acciones principales.

---

## 🎯 Criterios de Aceptación (Gherkin)

`gherkin
Escenario: Negocio con citas registradas
  Dado que el negocio tiene citas registradas en la base de datos
  Cuando el administrador entra al Dashboard
  Entonces el sistema calcula reactivamente el conteo de citas de hoy, clientes únicos, ingresos y pendientes
  Y lista las próximas citas ordenadas cronológicamente con su estado y servicio.

Escenario: Negocio nuevo sin citas (Estado Vacío)
  Dado que el negocio no tiene citas registradas
  Cuando el administrador ingresa al Dashboard
  Entonces las métricas inician en 0 / 
  Y la sección Próximas Citas muestra una tarjeta ilustrada con Aún no tienes citas registradas
  Y ofrece botones directos para Crear Cita e ir a la agenda o Compartir Link.
`

---

## 🔗 Componentes Asociados
* **Backend:** [[CalendarController]], GetCalendarSlotsQuery
* **Frontend:** DashboardScreen.tsx, useCalendarStore
