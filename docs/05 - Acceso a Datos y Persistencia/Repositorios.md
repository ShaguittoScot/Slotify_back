---
title: Repositorios (Patrón Repository)
tags:
  - efcore
  - repository
  - data-access
---

# 📂 Repositorios de Datos

Ubicación: `src/Slotify.Infrastructure/Data/Repositories/`

Encapsulan la lógica de consulta y manipulación de datos con Entity Framework Core, ocultando detalles de SQL a la capa de aplicación.

---

## 🗂️ Repositorios Implementados

### 1. `ScheduleBlockRepository`
* `GetByDateRangeAsync(Guid businessId, DateTime startDate, DateTime endDate, CancellationToken ct)`
* `GetByIdAsync(long id, CancellationToken ct)`
* `AddAsync(ScheduleBlock block, CancellationToken ct)`
* `DeleteAsync(ScheduleBlock block, CancellationToken ct)`
* Relacionado con: [[Feature ScheduleBlocks]], [[Entidad ScheduleBlock]].

### 2. `AppointmentRepository`
* Consultas por rango de fechas, cliente o empleado.
* Relacionado con: [[Feature Calendar]], [[Entidad Appointment]].

### 3. `UserRepository` / `BusinessRepository`
* Consultas por email, validación de existencia y altas.
* Relacionado con: [[Feature Auth]], [[Entidad User]], [[Entidad Business]].

---

## 🔗 Relaciones en el Grafo
- **Interfaces en:** [[Modulo Domain]]
- **Implementaciones en:** [[Modulo Infrastructure]]
- **Utilizan:** [[AppDbContext]]
- **Agrupados por:** [[Unit of Work]]
