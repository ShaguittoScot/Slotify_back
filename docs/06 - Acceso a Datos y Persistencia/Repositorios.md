---
title: Repositorios (Patrón Repository)
tags:
  - efcore
  - repository
  - data-access
  - persistence
  - infrastructure
---

# 📂 Repositorios de Datos

* **Ubicación en Código:** `src/Slotify.Infrastructure/Data/Repositories/`
* **Contratos (Interfaces):** `src/Slotify.Domain/Interfaces/`
* **Registro en DI:** `AddInfrastructureServices()` en [DependencyInjection.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/DependencyInjection.cs)

Los **Repositorios** encapsulan las operaciones de consulta y persistencia sobre la base de datos PostgreSQL mediante **Entity Framework Core 9**, aislando la lógica de acceso a datos de los casos de uso en la capa de aplicación.

---

## 🗂️ Repositorios e Interfaces Implementadas

### 1. `ScheduleBlockRepository` (`IScheduleBlockRepository`)
* Consultas por rango de fechas UTC y negocio: `GetByDateRangeAsync(Guid businessId, DateTime startDate, DateTime endDate, CancellationToken ct)`.
* Consulta por clave primaria: `GetByIdAsync(long id, CancellationToken ct)`.
* Altas y bajas: `AddAsync(ScheduleBlock block, CancellationToken ct)`, `DeleteAsync(ScheduleBlock block, CancellationToken ct)`.
* Relacionado con: [[Feature ScheduleBlocks]], [[Entidad ScheduleBlock]].

### 2. `AppointmentRepository` (`IAppointmentRepository`)
* Consultas por rango temporal de citas activas del negocio o colaborador.
* Persistencia y validación de citas.
* Relacionado con: [[Feature Calendar]], [[Entidad Appointment]].

### 3. `UserRepository` (`IUserRepository`)
* Consultas por correo electrónico (`GetByEmailAsync`), validación de existencia (`EmailExistsAsync`) y búsqueda por ID (`GetByIdAsync`).
* Relacionado con: [[Feature Auth]], [[Entidad User]].

### 4. `BusinessRepository` (`IBusinessRepository`)
* Consulta de negocios con detalles de plantilla de sector, servicios y marca: `GetAllWithDetailsAsync(CancellationToken ct)`.
* Búsqueda por slug (`GetBySlugAsync`), validación de existencia y altas.
* Relacionado con: [[Feature Businesses]], [[Entidad Business]].

### 5. `ClientRepository` (`IClientRepository`)
* Búsqueda por UID de Supabase (`GetBySupabaseIdAsync`) o por correo (`GetByEmailAsync`).
* Alta idempotente de consumidores finales.
* Relacionado con: [[Feature Auth]], [[Entidad Client]].

### 6. `SectorTemplateRepository` (`ISectorTemplateRepository`)
* Consulta del catálogo de plantillas de giro comercial: `GetAllAsync(CancellationToken ct)`.
* Relacionado con: [[Feature SectorTemplates]], [[Entidad SectorTemplate]].

### 7. Repositorio Genérico `Repository<T, TId>` (`IRepository<T, TId>`)
* Provee operaciones CRUD genéricas (`GetByIdAsync`, `ListAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`) para entidades del modelo.

### 8. Persistencia Especializada: `BusinessOnboardingPersistence` (`IBusinessOnboardingPersistence`)
* Coordina la transacción integral del alta comercial en 5 pasos ejecutando `ExecuteInTransactionAsync`, agrupando la inserción atómica de negocio, usuario, horarios y catálogo de servicios.
* Relacionado con: [[Feature Onboarding]].

---

## 🏛️ Inyección de Dependencias y Desacoplamiento

> [!important] Desacoplamiento con `IUnitOfWork`
> En Clean Architecture, los repositorios **no se acceden como propiedades dentro de `UnitOfWork`**. Cada repositorio se inyecta de forma independiente mediante Inyección de Dependencias (DI) directamente en los CQRS Handlers que lo requieren.
> 
> La interfaz [[06 - Acceso a Datos y Persistencia/Unit of Work|IUnitOfWork]] tiene como rol exclusivo coordinar la confirmación atómica de transacciones compartidas mediante `SaveChangesAsync(cancellationToken)`.

---

## 🔗 Relaciones Arquitectónicas
- **Interfaces de Dominio:** [[Modulo Domain]]
- **Implementaciones:** [[Modulo Infrastructure]]
- **Contexto Central:** [[AppDbContext]]
- **Coordinador Transaccional:** [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]]
- **Consumo:** [[Modulo Application]] ([[Feature Auth]], [[Feature Businesses]], [[Feature Calendar]], [[Feature ScheduleBlocks]], [[Feature Onboarding]])
- **Modelo Relacional:** [[Modelo de Datos]], [[Base de datos - Proyecto]]
