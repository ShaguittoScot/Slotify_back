---
title: US-004 - Dashboard Dinamico y Manejo de Estados Vacios
aliases:
  - US-004
  - Dashboard y Estados Vacíos
tags:
  - historia-usuario
  - dashboard
  - metricas
  - estado-vacio
  - us-004
estado_documental: "Original verificada"
implementacion_backend: "No implementada"
integracion_frontend: "Integrada"
---

# 📊 US-004: Dashboard Dinámico y Manejo de Estados Vacíos

* **Como:** Administrador / Dueño de negocio en Slotify.
* **Quiero:** Visualizar un resumen operativo con métricas clave en tiempo real (*Citas Hoy, Clientes, Ingresos Hoy, Pendientes*) y la lista de próximas citas o su correspondiente estado vacío.
* **Para:** Evaluar el rendimiento diario de mi negocio y acceder rápidamente a acciones principales desde el dispositivo móvil.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Negocio con citas registradas en el día
```gherkin
Dado que el negocio tiene citas registradas en el período actual
Cuando el administrador entra a la pantalla de Dashboard
Entonces el frontend calcula en el cliente las métricas (citas de hoy, clientes únicos, ingresos acumulados y pendientes)
Y lista las próximas citas ordenadas cronológicamente con su estado, hora y cliente.
```

### Escenario 2: Negocio nuevo sin citas registradas (Estado Vacío)
```gherkin
Dado que el negocio no tiene citas registradas en la fecha actual
Cuando el administrador ingresa al Dashboard
Entonces las métricas numéricas inician en 0
Y la sección de Próximas Citas muestra una tarjeta de estado vacío ilustrada ("Aún no tienes citas registradas")
Y ofrece accesos directos para agendar una cita o compartir el enlace de reservas.
```

---

## ⚙️ Implementación Backend Real

* **Controlador y Rutas:** No existe un controlador dedicado ni endpoints específicos en el backend (por ejemplo `/api/dashboard/metrics` o `/api/dashboard/summary`).
* **Soporte Indirecto:** El backend da soporte indirecto a través de `GET /api/calendar/slots` en `CalendarController.cs`.
* **Entidades Involucradas:** [[Entidad Business]], [[Entidad Appointment]]
* **Tablas de Base de Datos Impactadas:** `citas` (consultadas mediante el rango de fechas de calendario)

---

## 📱 Estado de Integración con el Frontend

* **Componentes UI:** `DashboardScreen.tsx`, `EmptyStateCard.tsx`, `useCalendarStore`
* **Nivel de Integración:** Integrada en el cliente.
* **Detalle Arquitectónico:**
  - El frontend calcula de forma puramente reactiva los KPIs agregados a partir del array de slots retornado por `useCalendarStore` (`GET /api/calendar/slots`).
  - No se realizan peticiones HTTP dedicadas de agregación hacia el servidor.
  - En caso de requerir alto volumen de citas o agregaciones históricas complejas en el futuro, se deberá diseñar un endpoint dedicado de analítica en el backend.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas Frontend:** Pruebas de renderizado de `EmptyStateCard` y cálculo de estados en `DashboardScreen`.
* **Pruebas Backend:** Pruebas de retorno de slots vacíos en `ProtectedEndpointsTests.cs`.

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-003 - Calendario Tactil Multiformato y Consulta de Citas]]
* [[Feature Calendar]]
* [[Modulo API]]
* [[Flujo de Consulta de Citas y Estado Vacio]]
* [[Entidad Business]]
* [[Entidad Appointment]]
