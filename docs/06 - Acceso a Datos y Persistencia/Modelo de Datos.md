---
title: Modelo de Datos Relacional
aliases:
  - Modelo de Datos
  - ERD
  - Esquema Relacional
tags:
  - base-de-datos
  - postgresql
  - supabase
  - efcore
  - erd
---

# 🗄️ Modelo de Datos Relacional (PostgreSQL / Supabase)

> [!abstract] Resumen del Modelo
> El backend opera sobre una base de datos relacional **PostgreSQL** administrada en **Supabase** y mapeada mediante **Entity Framework Core 9 (Npgsql)**. Esta nota documenta con precisión las tablas físicas reales, sus relaciones verificadas en código y la separación entre las entidades activas de EF Core y el diseño extendido del DDL.

---

## 📊 1. Diagrama Entidad-Relación Activo (EF Core 9)

Este diagrama representa estrictamente las **9 tablas administradas por la solución .NET** a través de [[AppDbContext]] y sus clases `IEntityTypeConfiguration<T>`:

```mermaid
erDiagram
    plantillas_sector ||--o{ negocios : "id_plantilla_sector (ON DELETE SET NULL)"
    negocios ||--|| marca_negocio : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ usuarios : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ servicios : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ horarios_disponibilidad : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ bloqueos_agenda : "id_negocio (ON DELETE CASCADE)"
    negocios ||--o{ citas : "id_negocio (ON DELETE CASCADE)"
    clientes ||--o{ citas : "id_cliente"

    plantillas_sector {
        int id PK
        varchar nombre
        jsonb modulos_defecto
        jsonb servicios_sugeridos
        jsonb configuracion_formulario
    }

    negocios {
        uuid id PK
        varchar slug UK
        varchar nombre
        varchar telefono
        varchar zona_horaria
        int anticipacion_minima_horas
        int anticipacion_maxima_dias
        int id_plantilla_sector FK
        jsonb configuracion_formulario_personalizada
        timestamptz onboarding_completed_at
        timestamp fecha_creacion
        timestamp fecha_actualizacion
    }

    marca_negocio {
        uuid id_negocio PK_FK
        text url_logotipo
        varchar color_primario
        boolean es_predeterminado
        timestamp fecha_actualizacion
    }

    usuarios {
        uuid id PK
        uuid id_negocio FK
        varchar nombre
        varchar apellido
        varchar correo UK
        varchar password_hash
        varchar rol
        boolean esta_activo
        boolean correo_verificado
        timestamp ultimo_inicio_sesion
        timestamp fecha_creacion
        timestamp fecha_actualizacion
    }

    servicios {
        uuid id PK
        uuid id_negocio FK
        varchar nombre
        int duracion_minutos
        int tiempo_colchon_minutos
        numeric precio
        boolean esta_activo
        timestamp fecha_eliminacion
    }

    horarios_disponibilidad {
        bigint id PK
        uuid id_negocio FK
        uuid id_empleado FK
        smallint dia_semana
        time hora_inicio
        time hora_fin
    }

    bloqueos_agenda {
        bigint id PK
        uuid id_negocio FK
        uuid id_empleado FK
        timestamp fecha_hora_inicio
        timestamp fecha_hora_fin
        varchar motivo
    }

    citas {
        uuid id PK
        uuid id_negocio FK
        uuid id_empleado FK
        uuid id_cliente FK
        timestamp hora_inicio
        timestamp hora_fin
        varchar nombre_cliente
        varchar correo_cliente
        varchar telefono_cliente
        varchar estado
        varchar token_cancelacion UK
        numeric total_pactado
        timestamp fecha_creacion
        timestamp fecha_actualizacion
    }

    clientes {
        uuid id PK
        uuid supabase_id UK
        varchar nombre
        varchar apellido
        varchar correo UK
        varchar telefono
        timestamp fecha_creacion
        timestamp fecha_actualizacion
    }
```

---

## 🧩 2. Detalle de Relaciones Implementadas en EF Core

| Origen | Destino | Clave Foránea | Cardinalidad | Comportamiento Eliminación | Propósito |
| :--- | :--- | :--- | :---: | :---: | :--- |
| `negocios` | `plantillas_sector` | `id_plantilla_sector` | N:1 | `SetNull` | Vincula el rubro comercial inicial ([[Entidad SectorTemplate]]). |
| `marca_negocio` | `negocios` | `id_negocio` | 1:1 | `Cascade` | Extensión de marca y colores corporativos ([[Entidad BusinessBrand]]). |
| `usuarios` | `negocios` | `id_negocio` | N:1 | `Cascade` | Propietarios y empleados vinculados a su tenant ([[Entidad User]]). |
| `servicios` | `negocios` | `id_negocio` | N:1 | `Cascade` | Catálogo de servicios ofrecidos ([[Entidad Service]]). |
| `horarios_disponibilidad`| `negocios` | `id_negocio` | N:1 | `Cascade` | Horarios regulares de apertura ([[Entidad AvailabilitySchedule]]). |
| `bloqueos_agenda` | `negocios` | `id_negocio` | N:1 | `Cascade` | Inhabilitaciones manuales de tiempo ([[Entidad ScheduleBlock]]). |
| `citas` | `negocios` | `id_negocio` | N:1 | `Cascade` | Agenda de citas del establecimiento ([[Entidad Appointment]]). |
| `citas` | `clientes` | `id_cliente` | N:1 | `Restrict` / Convención | Vínculo opcional con el perfil B2C ([[Entidad Client]]). |

---

## 📦 3. Tablas Existentes en el DDL no Mapeadas en EF Core

El archivo `Base de datos - Proyecto.md` define un schema de 16 tablas. Las siguientes **7 tablas existen a nivel de diseño relacional / script SQL** pero actualmente **no tienen entidades ni DbSets en C#**:

1. **`modulos`:** Catálogo de características funcionales globales del sistema (citas, CRM, inventario, etc.).
2. **`modulos_negocio`:** Tabla pivote para activar o desactivar módulos por cada negocio.
3. **`empleados`:** Perfil extendido con comisión, color de calendario y biografía (actualmente los empleados se identifican mediante `usuarios.rol = 'EMPLEADO'`).
4. **`dispositivos_push`:** Tokens de Expo / FCM para notificaciones móviles push.
5. **`sesiones_usuario`:** Registro de tokens de refresco y auditoría de accesos.
6. **`servicios_empleados`:** Tabla pivote N:M para asignar qué empleados atienden qué servicios específicos.
7. **`citas_servicios`:** Detalle N:M de servicios incluidos por cita (actualmente la cita maneja `total_pactado` de forma consolidada).

---

## 🧭 4. Relaciones Conceptuales Pendientes de Implementación

1. **Citas Multi-Servicio:** Actualmente la cita es atómica con un precio pactado global (`total_pactado`). La relación N:M hacia `servicios` vía `citas_servicios` está en el DDL pero no en la API.
2. **Asignación Especializada de Empleados a Servicios:** Actualmente cualquier servicio del negocio puede ser atendido por el personal del negocio. La relación `servicios_empleados` permitirá restringir turnos según especialidad.
3. **Asociación de Empleados en Horarios y Citas:** Las columnas `id_empleado` en `citas`, `bloqueos_agenda` y `horarios_disponibilidad` son `nullable` y apuntan físicamente a `usuarios.id`, pero la navegación completa de empleados como agregados independientes está proyectada para fases posteriores.

---

## 🔗 Enlaces Relacionados
* [[AppDbContext]]
* [[Supabase PostgreSQL]]
* [[Base de datos - Proyecto]]
* [[Entidad Business]]
* [[Entidad User]]
* [[Entidad Client]]
* [[Entidad Appointment]]
* [[Entidad Service]]
* [[Entidad BusinessBrand]]
* [[Entidad ScheduleBlock]]
* [[Entidad AvailabilitySchedule]]
* [[Entidad SectorTemplate]]
