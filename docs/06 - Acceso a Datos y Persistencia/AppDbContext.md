---
title: AppDbContext (Entity Framework Core)
aliases:
  - AppDbContext
  - DbContext
tags:
  - efcore
  - persistence
  - orm
  - infrastructure
  - database
---

# ⚡ `AppDbContext` (Entity Framework Core 9)

> [!abstract] Resumen del DbContext
> `AppDbContext` es el contexto central de persistencia relacional en **Entity Framework Core 9 (Npgsql)**. Conecta las entidades de [[Modulo Domain]] con las tablas físicas de [[Supabase PostgreSQL]], orquesta la auditoría temporal automática y aplica todas las configuraciones Fluent API mediante reflexión de ensamblado.

---

## 📍 Metadatos Técnicos
* **Archivo:** `src/Slotify.Infrastructure/Data/AppDbContext.cs`
* **Namespace:** `Slotify.Infrastructure.Data`
* **Clase Base:** `Microsoft.EntityFrameworkCore.DbContext`
* **Proveedor de Base de Datos:** `Npgsql.EntityFrameworkCore.PostgreSQL`
* **Inyección de Dependencias:** Registrado con ciclo de vida `Scoped` en `Slotify.Infrastructure/DependencyInjection.cs`.

---

## 🗄️ Tabla de Correspondencia: DbSets vs Entidades vs Tablas

A continuación se detallan las **nueve (9) entidades reales** gestionadas por `AppDbContext`:

| DbSet | Entidad de Dominio | Tabla PostgreSQL | Clase de Configuración Fluent API |
| :--- | :--- | :--- | :--- |
| `Users` | [[Entidad User]] | `"usuarios"` | `UserConfiguration.cs` |
| `Businesses` | [[Entidad Business]] | `"negocios"` | `BusinessConfiguration.cs` |
| `SectorTemplates` | [[Entidad SectorTemplate]] | `"plantillas_sector"` | `SectorTemplateConfiguration.cs` |
| `AvailabilitySchedules` | [[Entidad AvailabilitySchedule]] | `"horarios_disponibilidad"` | `AvailabilityScheduleConfiguration.cs` |
| `ScheduleBlocks` | [[Entidad ScheduleBlock]] | `"bloqueos_agenda"` | `ScheduleBlockConfiguration.cs` |
| `Appointments` | [[Entidad Appointment]] | `"citas"` | `AppointmentConfiguration.cs` |
| `Clients` | [[Entidad Client]] | `"clientes"` | `ClientConfiguration.cs` |
| `BusinessBrands` | [[Entidad BusinessBrand]] | `"marca_negocio"` | `BusinessBrandConfiguration.cs` |
| `Services` | [[Entidad Service]] | `"servicios"` | `ServiceConfiguration.cs` |

> [!tip] Correspondencia Completa de Persistencia
> La totalidad de las nueve (9) tablas relacionales mapeadas por `AppDbContext` se encuentran documentadas con sus tipos, restricciones, claves foráneas e índices en [[Modelo de Datos]] y [[Base de datos - Proyecto]].

---

## ⚙️ Características y Mecanismos Internos

### 1. Carga Automática de Configuraciones Fluent API
En lugar de saturar `OnModelCreating` con llamadas manuales, el contexto escanea automáticamente el ensamblado en busca de todas las implementaciones de `IEntityTypeConfiguration<T>`:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

### 2. Auditoría Temporal Automática (`SaveChangesAsync`)
Interpreta las entidades que heredan de `AuditableEntity` inspeccionando el `ChangeTracker` antes de enviar la transacción a PostgreSQL:
* **Entidades con estado `EntityState.Added`:** Se inicializa `CreatedAt = DateTime.UtcNow`.
* **Entidades con estado `EntityState.Modified`:** Se actualiza `UpdatedAt = DateTime.UtcNow`.

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = DateTime.UtcNow;
                break;
            case EntityState.Modified:
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                break;
        }
    }
    return await base.SaveChangesAsync(cancellationToken);
}
```

---

## 🔗 Relaciones en el Grafo
- **Capa:** [[Modulo Infrastructure]]
- **Consumido por:** [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]], [[Repositorios]]
- **Conecta con:** [[Supabase PostgreSQL]]
- **Referencia entidades de:** [[Modulo Domain]]
- **Mapeo detallado:** [[Modelo de Datos]]
