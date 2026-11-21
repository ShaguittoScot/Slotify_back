---
title: Modelo de Base de Datos del Proyecto (Esquema Relacional)
tags:
  - base-de-datos
  - er-diagram
  - proyecto
  - sql
  - persistencia
  - efcore
  - postgresql
---

# 🗄️ Modelo de Base de Datos del Proyecto (Esquema Relacional)

> [!abstract] Resumen del Modelo
> Esquema relacional multi-inquilino (*multi-tenant*) diseñado para la gestión integral de reservas, catálogos de servicios comerciales, bloqueos de disponibilidad, personalización de formularios y sincronización de identidades entre **Supabase Auth** y **PostgreSQL**. Administrado en el backend mediante **ASP.NET Core .NET 9** y **Entity Framework Core (Npgsql)**.

---

## 🎯 1. Marco Metodológico: Niveles de Abstracción de Datos

Para garantizar rigor y trazabilidad técnica, este documento clasifica cada componente del modelo en tres niveles estrictos:

1. **Entidad de Dominio:** Clase pura en C# ubicada en el proyecto `Slotify.Domain/Entities/`. Encapsula el estado y las reglas de negocio sin acoplamiento a persistencia.
2. **Entidad Mapeada:** Entidad registrada en `AppDbContext` mediante `DbSet<T>` y configurada en `Slotify.Infrastructure/Data/Configurations/` con Fluent API (`IEntityTypeConfiguration<T>`).
3. **Tabla Física (PostgreSQL):** Tabla relacional persistida en el motor PostgreSQL de Supabase.
   > [!note] Estado de Verificación Física y Mecanismo de Migración
   > - **Ausencia de Migraciones EF Core:** La solución no contiene clases de migración automática (`dotnet ef migrations`; no existe carpeta `Migrations/`).
   > - **Sentencias DDL al Inicio:** En `Program.cs` se ejecutan sentencias SQL raw defensivas durante el arranque:
   >   - `ALTER TABLE negocios ADD COLUMN IF NOT EXISTS configuracion_formulario_personalizada JSONB;`
   >   - `ALTER TABLE negocios ADD COLUMN IF NOT EXISTS onboarding_completed_at TIMESTAMPTZ NULL;`
   >   *(Nota: no existe sentencia de migración en arranque para `timezone`, la cual se mapea como propiedad regular de la entidad `Business`).*
   > - **Verificación Física Pendiente:** En esta fase de auditoría no se ejecutó una inspección interactiva directa (`information_schema.tables`) sobre el servidor PostgreSQL de producción. Las tablas y columnas documentadas corresponden estrictamente al mapeo Fluent API verificado en código C#.

---

## 📊 2. Diagrama Entidad-Relación Integral (Mermaid ERD)

El siguiente diagrama modela las **nueve (9) entidades relacionales mapeadas** por la solución .NET 9 a través de [[AppDbContext]] y sus configuraciones Fluent API:

```mermaid
erDiagram
    plantillas_sector ||--o{ negocios : "id_plantilla_sector (ON DELETE SET NULL)"
    negocios ||--|| marca_negocio : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ usuarios : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ servicios : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ horarios_disponibilidad : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ bloqueos_agenda : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ citas : "id_negocio (ON DELETE CASCADE)"
    clientes ||--o{ citas : "id_cliente (ON DELETE SET NULL)"

    plantillas_sector {
        int id PK "SERIAL"
        varchar nombre "NOT NULL, max 100"
        jsonb modulos_defecto "NOT NULL, default '[]'"
        jsonb servicios_sugeridos "NOT NULL, default '[]'"
        jsonb configuracion_formulario "NOT NULL, default '{}'"
    }

    negocios {
        uuid id PK "default uuid_generate_v4()"
        varchar slug UK "NOT NULL, max 100"
        varchar nombre "NOT NULL, max 150"
        varchar telefono "max 20"
        varchar zona_horaria "NOT NULL, default 'America/Mexico_City'"
        int anticipacion_minima_horas "NOT NULL, default 2"
        int anticipacion_maxima_dias "NOT NULL, default 30"
        int id_plantilla_sector FK "nullable"
        jsonb configuracion_formulario_personalizada "nullable"
        timestamptz onboarding_completed_at "nullable"
        timestamp fecha_creacion "default CURRENT_TIMESTAMP"
        timestamp fecha_actualizacion "default CURRENT_TIMESTAMP"
    }

    marca_negocio {
        uuid id_negocio PK_FK "ON DELETE CASCADE"
        text url_logotipo "nullable"
        varchar color_primario "max 7, default '#000000'"
        boolean es_predeterminado "default true"
        timestamp fecha_actualizacion "default CURRENT_TIMESTAMP"
    }

    usuarios {
        uuid id PK
        uuid id_negocio FK "NOT NULL, ON DELETE CASCADE"
        varchar nombre "NOT NULL, max 100"
        varchar apellido "max 100"
        varchar correo UK "NOT NULL, max 150"
        varchar password_hash "NOT NULL, max 255"
        varchar rol "NOT NULL, DUENO o EMPLEADO"
        boolean esta_activo "default true"
        boolean correo_verificado "default false"
        timestamp ultimo_inicio_sesion "nullable"
        timestamp fecha_creacion "default CURRENT_TIMESTAMP"
        timestamp fecha_actualizacion "default CURRENT_TIMESTAMP"
    }

    servicios {
        uuid id PK "default uuid_generate_v4()"
        uuid id_negocio FK "NOT NULL, ON DELETE CASCADE"
        varchar nombre "NOT NULL, max 150"
        int duracion_minutos "NOT NULL"
        int tiempo_colchon_minutos "NOT NULL, default 0"
        numeric precio "precision 10_2, default 0.00"
        boolean esta_activo "default true"
        timestamp fecha_eliminacion "nullable"
    }

    horarios_disponibilidad {
        bigint id PK "generated on add"
        uuid id_negocio FK "NOT NULL, ON DELETE CASCADE"
        uuid id_empleado FK "nullable"
        smallint dia_semana "NOT NULL, 0=Dom a 6=Sab"
        time hora_inicio "NOT NULL"
        time hora_fin "NOT NULL"
    }

    bloqueos_agenda {
        bigint id PK "BIGSERIAL / Identity Always"
        uuid id_negocio FK "NOT NULL, ON DELETE CASCADE"
        uuid id_empleado FK "nullable"
        timestamp fecha_hora_inicio "NOT NULL"
        timestamp fecha_hora_fin "NOT NULL"
        varchar motivo "max 255"
    }

    citas {
        uuid id PK "default uuid_generate_v4()"
        uuid id_negocio FK "NOT NULL, ON DELETE CASCADE"
        uuid id_empleado FK "nullable"
        uuid id_cliente FK "nullable, ON DELETE SET NULL"
        timestamp hora_inicio "NOT NULL"
        timestamp hora_fin "NOT NULL"
        varchar nombre_cliente "NOT NULL, max 150"
        varchar correo_cliente "NOT NULL, max 150"
        varchar telefono_cliente "NOT NULL, max 20"
        varchar estado "NOT NULL, confirmed|cancelled|completed|no_show"
        varchar token_cancelacion UK "NOT NULL, max 64"
        numeric total_pactado "precision 10_2, default 0.00"
        timestamp fecha_creacion "default CURRENT_TIMESTAMP"
        timestamp fecha_actualizacion "default CURRENT_TIMESTAMP"
    }

    clientes {
        uuid id PK
        uuid supabase_id UK "nullable"
        varchar nombre "NOT NULL, max 100"
        varchar apellido "max 100"
        varchar correo UK "NOT NULL, max 150"
        varchar telefono "max 30"
        timestamp fecha_creacion "default CURRENT_TIMESTAMP"
        timestamp fecha_actualizacion "default CURRENT_TIMESTAMP"
    }
```

---

## 🗂️ 3. Catálogo de Tablas, Entidades y Mapeo EF Core

| # | Entidad de Dominio | DbSet en `AppDbContext` | Tabla Mapeada | Archivo de Configuración | Clave Primaria (PK) |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **1** | [[Entidad User]] | `Users` | `"usuarios"` | [UserConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/UserConfiguration.cs) | `id` (UUID) |
| **2** | [[Entidad Business]] | `Businesses` | `"negocios"` | [BusinessConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/BusinessConfiguration.cs) | `id` (UUID) |
| **3** | [[Entidad SectorTemplate]] | `SectorTemplates` | `"plantillas_sector"` | [SectorTemplateConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/SectorTemplateConfiguration.cs) | `id` (int, SERIAL) |
| **4** | [[Entidad AvailabilitySchedule]] | `AvailabilitySchedules` | `"horarios_disponibilidad"` | [AvailabilityScheduleConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/AvailabilityScheduleConfiguration.cs) | `id` (bigint) |
| **5** | [[Entidad ScheduleBlock]] | `ScheduleBlocks` | `"bloqueos_agenda"` | [ScheduleBlockConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/ScheduleBlockConfiguration.cs) | `id` (bigint, Identity) |
| **6** | [[Entidad Appointment]] | `Appointments` | `"citas"` | [AppointmentConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/AppointmentConfiguration.cs) | `id` (UUID) |
| **7** | [[Entidad Client]] | `Clients` | `"clientes"` | [ClientConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/ClientConfiguration.cs) | `id` (UUID) |
| **8** | [[Entidad BusinessBrand]] | `BusinessBrands` | `"marca_negocio"` | [BusinessBrandConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/BusinessBrandConfiguration.cs) | `id_negocio` (UUID) |
| **9** | [[Entidad Service]] | `Services` | `"servicios"` | [ServiceConfiguration.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/Configurations/ServiceConfiguration.cs) | `id` (UUID) |

---

## 🔍 4. Especificación Técnica por Tabla

### 4.1 Tabla `negocios`
Representa al inquilino comercial (*Tenant*). Es la raíz de particionamiento lógico para usuarios, servicios, citas y disponibilidades.
- **`id`** (`UUID`, PK): Identificador único con valor predeterminado `uuid_generate_v4()`.
- **`slug`** (`VARCHAR(100)`, UK, Not Null): Identificador amigable y único para URLs públicas (ej. `barberia-central`).
- **`nombre`** (`VARCHAR(150)`, Not Null): Razón social o nombre comercial.
- **`telefono`** (`VARCHAR(20)`, Nullable): Teléfono de contacto comercial.
- **`zona_horaria`** (`VARCHAR(50)`, Not Null): Zona horaria IANA, por defecto `'America/Mexico_City'`.
- **`anticipacion_minima_horas`** (`INT`, Not Null): Horas de antelación para agendar, por defecto `2`.
- **`anticipacion_maxima_dias`** (`INT`, Not Null): Días máximos hacia el futuro para reservar, por defecto `30`.
- **`id_plantilla_sector`** (`INT`, FK, Nullable): Referencia a `plantillas_sector(id)`. Si la plantilla se elimina, se asigna `NULL` (`OnDelete(DeleteBehavior.SetNull)`).
- **`configuracion_formulario_personalizada`** (`JSONB`, Nullable): Estructura JSON con campos dinámicos de reserva.
- **`onboarding_completed_at`** (`TIMESTAMPTZ`, Nullable): Marca de tiempo de culminación del asistente de alta.
- **`fecha_creacion` / `fecha_actualizacion`** (`TIMESTAMP`, Not Null): Auditoría temporal UTC.

### 4.2 Tabla `usuarios`
Personal interno del negocio (administradores y colaboradores).
- **`id`** (`UUID`, PK): Coincide con el UID emitido por Supabase Auth (`auth.users.id`).
- **`id_negocio`** (`UUID`, FK, Not Null): Referencia obligatoria a `negocios(id)` con eliminación en cascada (`OnDelete(DeleteBehavior.Cascade)`).
- **`nombre`** (`VARCHAR(100)`, Not Null) y **`apellido`** (`VARCHAR(100)`, Nullable). La propiedad `FullName` está ignorada en el mapeo (`builder.Ignore(x => x.FullName)`).
- **`correo`** (`VARCHAR(150)`, UK, Not Null): Correo electrónico corporativo único.
- **`password_hash`** (`VARCHAR(255)`, Not Null): Almacena `'managed_by_supabase'`.
- **`rol`** (`VARCHAR(20)`, Not Null): Conversión de enum `UserRole` (`UserRole.Owner` ↔ `"DUENO"`, `UserRole.Employee` ↔ `"EMPLEADO"`).
- **`esta_activo`** (`BOOLEAN`, Not Null): Por defecto `true`.
- **`correo_verificado`** (`BOOLEAN`, Not Null): Por defecto `false`.
- **`ultimo_inicio_sesion`** (`TIMESTAMP`, Nullable).

### 4.3 Tabla `clientes`
Consumidores finales de la plataforma (B2C). A diferencia de los usuarios, no pertenecen a un inquilino único; interactúan transversalmente con múltiples negocios.
- **`id`** (`UUID`, PK): UUID asignado al cliente.
- **`supabase_id`** (`UUID`, UK, Nullable): Enlace opcional y único con la cuenta en Supabase Auth (`auth.users.id`).
- **`nombre`** (`VARCHAR(100)`, Not Null) y **`apellido`** (`VARCHAR(100)`, Nullable). `FullName` ignorado en EF Core.
- **`correo`** (`VARCHAR(150)`, UK, Not Null): Correo electrónico personal.
- **`telefono`** (`VARCHAR(30)`, Nullable): Teléfono de contacto para avisos de reserva.

### 4.4 Tabla `servicios`
Catálogo de servicios comerciales ofrecidos por cada negocio.
- **`id`** (`UUID`, PK): Con valor predeterminado `uuid_generate_v4()`.
- **`id_negocio`** (`UUID`, FK, Not Null): Referencia a `negocios(id)` con eliminación en cascada.
- **`nombre`** (`VARCHAR(150)`, Not Null): Nombre del servicio (ej. "Corte y Barba").
- **`duracion_minutos`** (`INT`, Not Null): Duración estimada de atención en minutos.
- **`tiempo_colchon_minutos`** (`INT`, Not Null): Tiempo de preparación entre citas, por defecto `0`.
- **`precio`** (`NUMERIC(10,2)`, Not Null): Precio unitario, por defecto `0.00`.
- **`esta_activo`** (`BOOLEAN`, Not Null): Por defecto `true`.
- **`fecha_eliminacion`** (`TIMESTAMP`, Nullable): Soporte para borrado lógico (*soft delete*).

### 4.5 Tabla `citas`
Reservaciones de atención concertadas entre un cliente y el negocio.
- **`id`** (`UUID`, PK): UUID generado por defecto.
- **`id_negocio`** (`UUID`, FK, Not Null): Negocio donde se ejecutará la cita (`OnDelete(DeleteBehavior.Cascade)`).
- **`id_empleado`** (`UUID`, FK, Nullable): Colaborador asignado.
- **`id_cliente`** (`UUID`, FK, Nullable): Enlace con `clientes(id)` mediante eliminación `SetNull`.
- **`hora_inicio` / `hora_fin`** (`TIMESTAMP`, Not Null): Rango horario reservado.
- **`nombre_cliente`**, **`correo_cliente`**, **`telefono_cliente`** (`VARCHAR`, Not Null): Datos de contacto directos congelados al agendar.
- **`estado`** (`VARCHAR(20)`, Not Null): Mapeo con enum `AppointmentStatus`:
  - `"confirmed"`, `"cancelled"`, `"completed"`, `"no_show"`.
- **`token_cancelacion`** (`VARCHAR(64)`, UK, Not Null): Token criptográfico para autoservicio de cancelación sin login.
- **`total_pactado`** (`NUMERIC(10,2)`, Not Null): Monto acordado para la cita.
- **Índices de Rendimiento:**
  - `idx_citas_calendario`: Índice compuesto sobre `(id_negocio, hora_inicio, hora_fin)`.
  - `idx_citas_empleado`: Índice compuesto sobre `(id_empleado, hora_inicio, hora_fin)`.

### 4.6 Tabla `bloqueos_agenda`
Restricciones manuales de horario en las que no se pueden concertar citas.
- **`id`** (`BIGINT`, PK): Clave numérica incremental administrada con `UseIdentityAlwaysColumn()`.
- **`id_negocio`** (`UUID`, FK, Not Null): Negocio al que aplica el bloqueo (`OnDelete(DeleteBehavior.Cascade)`).
- **`id_empleado`** (`UUID`, FK, Nullable): Si es nulo, bloquea toda la agenda del local; si tiene valor, bloquea al colaborador específico.
- **`fecha_hora_inicio` / `fecha_hora_fin`** (`TIMESTAMP`, Not Null): Intervalo UTC del bloqueo.
- **`motivo`** (`VARCHAR(255)`, Nullable): Causa del bloqueo (ej. "Almuerzo de equipo").

### 4.7 Tabla `horarios_disponibilidad`
Jornada de apertura regular del negocio o de un empleado para cada día de la semana.
- **`id`** (`BIGINT`, PK): Clave generada al insertar.
- **`id_negocio`** (`UUID`, FK, Not Null): Referencia a `negocios(id)` (`OnDelete(DeleteBehavior.Cascade)`).
- **`id_empleado`** (`UUID`, FK, Nullable): Colaborador asignado o nulo si es el horario general del local.
- **`dia_semana`** (`SMALLINT`, Not Null): Día de la semana (0 = Domingo, 1 = Lunes, ..., 6 = Sábado).
- **`hora_inicio` / `hora_fin`** (`TIME WITHOUT TIME ZONE`, Not Null): Horas de apertura y cierre en formato `TimeOnly`.

### 4.8 Tabla `marca_negocio`
Identidad visual y de marca corporativa (relación 1:1 estricta con `negocios`).
- **`id_negocio`** (`UUID`, PK y FK): Actúa simultáneamente como clave primaria y clave foránea referenciando a `negocios(id)` (`OnDelete(DeleteBehavior.Cascade)`).
- **`url_logotipo`** (`TEXT`, Nullable): Enlace al activo gráfico del logotipo.
- **`color_primario`** (`VARCHAR(7)`, Not Null): Color corporativo hexadecimal, por defecto `'#000000'`.
- **`es_predeterminado`** (`BOOLEAN`, Not Null): Por defecto `true`.
- **`fecha_actualizacion`** (`TIMESTAMP`, Not Null): Por defecto `CURRENT_TIMESTAMP`.

### 4.9 Tabla `plantillas_sector`
Catálogo preconfigurado de giros comerciales (Barbería, Spa, Consultorio, etc.).
- **`id`** (`INT`, PK): Serial autoincremental (`UseSerialColumn()`).
- **`nombre`** (`VARCHAR(100)`, Not Null): Nombre del sector comercial.
- **`modulos_defecto`** (`JSONB`, Not Null): Lista JSON de módulos iniciales (`'[]'::jsonb`).
- **`servicios_sugeridos`** (`JSONB`, Not Null): Lista JSON de servicios sugeridos (`'[]'::jsonb`).
- **`configuracion_formulario`** (`JSONB`, Not Null): Plantilla inicial de formulario (`'{}'::jsonb`).

---

## 🏛️ 5. Patrones de Persistencia y Acceso a Datos

1. **Contexto Central:** [[AppDbContext]] aplica automáticamente todas las configuraciones Fluent API mediante `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)` y ejecuta auditoría temporal automática sobre `AuditableEntity`.
2. **Repositorios Desacoplados:** Implementados en `Slotify.Infrastructure/Data/Repositories/`. Inyectados independientemente por DI en los CQRS Handlers:
   - `UserRepository`, `BusinessRepository`, `AppointmentRepository`, `ScheduleBlockRepository`, `SectorTemplateRepository`, `ClientRepository`, `Repository<T>`.
3. **Unidad de Trabajo ([[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]]):** `IUnitOfWork` expone exclusivamente `Task<int> SaveChangesAsync(...)`, permitiendo confirmar de forma atómica todas las mutaciones realizadas por múltiples repositorios dentro del mismo ciclo de vida *Scoped*.
4. **Asistente Transaccional:** `IBusinessOnboardingPersistence` (`BusinessOnboardingPersistence.cs`) coordina la estrategia de ejecución `ExecuteInTransactionAsync` para dar de alta en una transacción atómica el negocio, usuario, horarios y servicios.

---

## 🔗 Relaciones con la Bóveda

- **Contexto EF Core:** [[AppDbContext]]
- **Diagrama y Resumen Relacional:** [[Modelo de Datos]]
- **Motor Cloud:** [[Supabase PostgreSQL]]
- **Patrón Transaccional:** [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]]
- **Repositorios:** [[Repositorios]]
- **Entidades de Dominio:** [[Entidad User]], [[Entidad Business]], [[Entidad SectorTemplate]], [[Entidad AvailabilitySchedule]], [[Entidad ScheduleBlock]], [[Entidad Appointment]], [[Entidad Client]], [[Entidad BusinessBrand]], [[Entidad Service]]
- **Arquitectura de Capas:** [[Modulo Infrastructure]], [[Modulo Domain]], [[Modulo Application]]
