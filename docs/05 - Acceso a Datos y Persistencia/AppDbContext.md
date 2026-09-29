---
title: AppDbContext (Entity Framework Core)
tags:
  - efcore
  - persistence
  - orm
---

# ⚡ `AppDbContext`

Ubicación: `src/Slotify.Infrastructure/Data/AppDbContext.cs`

El `AppDbContext` es el contexto principal de **Entity Framework Core 9** que mapea las entidades de [[Modulo Domain]] hacia las tablas de [[Supabase PostgreSQL]].

---

## 🗄️ DbSets Mapeados
* `Users` ➡️ `DbSet<User>` ([[Entidad User]])
* `Businesses` ➡️ `DbSet<Business>` ([[Entidad Business]])
* `SectorTemplates` ➡️ `DbSet<SectorTemplate>` ([[Entidad SectorTemplate]])
* `AvailabilitySchedules` ➡️ `DbSet<AvailabilitySchedule>` ([[Entidad AvailabilitySchedule]])
* `ScheduleBlocks` ➡️ `DbSet<ScheduleBlock>` ([[Entidad ScheduleBlock]])
* `Appointments` ➡️ `DbSet<Appointment>` ([[Entidad Appointment]])

---

## ⚙️ Características Clave
1. **Aplicación Automática de Configuraciones:**
   ```csharp
   modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
   ```
   Carga todas las clases `IEntityTypeConfiguration<T>` de la carpeta `Data/Configurations/`.

2. **Auditoría Automática (`SaveChangesAsync`):**
   Interpreta `ChangeTracker` para asignar automáticamente `CreatedAt` en inserciones y `UpdatedAt` en modificaciones con `DateTime.UtcNow`.

---

## 🔗 Relaciones en el Grafo
- **Parte de:** [[Modulo Infrastructure]]
- **Inyectado en:** [[Unit of Work]], [[Repositorios]]
- **Conecta con:** [[Supabase PostgreSQL]]
