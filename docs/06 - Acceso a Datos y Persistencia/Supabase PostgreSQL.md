---
title: Supabase PostgreSQL
tags:
  - database
  - postgresql
  - supabase
  - cloud
  - persistence
---

# ☁️ Supabase PostgreSQL

La base de datos relacional de **Slotify** está hospedada en la nube mediante **Supabase**, utilizando el motor relacional **PostgreSQL** y consumida por el backend a través de **Entity Framework Core 9 (Npgsql)**.

---

## 🔑 Parámetros de Conexión
* **Host:** `aws-0-us-east-1.pooler.supabase.com`
* **Puerto:** `5432`
* **Base de Datos:** `postgres`
* **Seguridad:** `SSL Mode=Require;Trust Server Certificate=true`
* **Configuración:** Almacenada de forma segura en las variables de entorno bajo `ConnectionStrings__DefaultConnection`.

---

## 📐 Distinción de Niveles de Persistencia

Para evitar ambigüedades entre el modelo de software y el almacenamiento real, el sistema distingue los siguientes niveles:

1. **Tablas Configuradas en EF Core:** Las nueve (9) tablas mapeadas explícitamente mediante `builder.ToTable(...)` en las clases `IEntityTypeConfiguration<T>`.
2. **Tablas Referenciadas en Migraciones:** Modificaciones dinámicas de esquema aplicadas mediante scripts o código de inicialización (por ejemplo, en `Program.cs` se ejecutan sentencias DDL directas sobre la tabla `negocios` para asegurar las columnas `configuracion_formulario_personalizada` y `onboarding_completed_at`).
3. **Tablas Físicas Verificadas en PostgreSQL:**
   > [!note] Estado de Verificación Física
   > En este ciclo de auditoría documental **no se estableció una conexión interactiva directa al servidor PostgreSQL en producción** para ejecutar consultas de inspección de catálogo (`information_schema.tables`). La existencia y diseño de las tablas se documenta a partir de las configuraciones activas de EF Core 9 y los comandos DDL del repositorio.

---

## 🗃️ Tablas Configuradas en EF Core

El backend interactúa directamente con **nueve (9) tablas relacionales** mapeadas mediante [[AppDbContext]]:

| Tabla Configurada (EF Core) | Entidad de Dominio | Responsabilidad / Contenido |
| :--- | :--- | :--- |
| `"usuarios"` | [[Entidad User]] | Administradores (`DUENO`) y colaboradores (`EMPLEADO`). |
| `"negocios"` | [[Entidad Business]] | Inquilinos comerciales (*Tenants*), slugs y configuración de formulario. |
| `"plantillas_sector"` | [[Entidad SectorTemplate]] | Catálogo preconfigurado de giros comerciales y módulos sugeridos. |
| `"horarios_disponibilidad"` | [[Entidad AvailabilitySchedule]] | Horarios de atención y apertura semanales del local o empleado. |
| `"bloqueos_agenda"` | [[Entidad ScheduleBlock]] | Excepciones y franjas de tiempo no disponibles en la agenda. |
| `"citas"` | [[Entidad Appointment]] | Reservaciones de atención concertadas con clientes. |
| `"clientes"` | [[Entidad Client]] | Consumidores finales (B2C) sincronizados con Supabase Auth. |
| `"marca_negocio"` | [[Entidad BusinessBrand]] | Logotipo y colores corporativos (relación 1:1 con negocio). |
| `"servicios"` | [[Entidad Service]] | Catálogo de servicios comerciales, duraciones y precios. |

---

## 🔗 Relaciones en el Grafo
- **Contexto Central:** [[AppDbContext]]
- **Modelo Relacional Detallado:** [[Modelo de Datos]], [[Base de datos - Proyecto]]
- **Patrón de Acceso:** [[Repositorios]], [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]]
- **Configuración de Pipeline:** [[Modulo API]], [[Modulo Infrastructure]]
- **Flujos Principales:** [[Flujo General de Peticion]], [[Flujo de Registro y Sync Auth]], [[Flujo de Bloqueo de Horarios]]
