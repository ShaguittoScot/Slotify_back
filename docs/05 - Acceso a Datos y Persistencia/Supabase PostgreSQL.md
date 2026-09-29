---
title: Supabase PostgreSQL
tags:
  - database
  - postgresql
  - supabase
  - cloud
---

# ☁️ Supabase PostgreSQL

La base de datos del sistema está hospedada en **Supabase** utilizando el motor relacional **PostgreSQL**.

---

## 🔑 Parámetros de Conexión
* **Host:** `aws-0-us-east-1.pooler.supabase.com`
* **Puerto:** `5432`
* **Base de Datos:** `postgres`
* **Seguridad:** `SSL Mode=Require;Trust Server Certificate=true`
* **Configuración:** Almacenada de forma segura en `.env` bajo `ConnectionStrings__DefaultConnection`.

---

## 🗃️ Tablas en la Base de Datos
* `"Users"` ➡️ [[Entidad User]]
* `"Businesses"` ➡️ [[Entidad Business]]
* `"ScheduleBlocks"` ➡️ [[Entidad ScheduleBlock]]
* `"Appointments"` ➡️ [[Entidad Appointment]]
* `"AvailabilitySchedules"` ➡️ [[Entidad AvailabilitySchedule]]
* `"SectorTemplates"` ➡️ [[Entidad SectorTemplate]]

---

## 🔗 Relaciones en el Grafo
- **Accedida por:** [[AppDbContext]]
- **Configurada en:** `.env`, [[Modulo API]]
- **Flujos:** [[Flujo General de Peticion]], [[Flujo de Registro y Sync Auth]], [[Flujo de Bloqueo de Horarios]]
