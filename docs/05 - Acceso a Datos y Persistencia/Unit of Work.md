---
title: Unit of Work (Patrón de Unidad de Trabajo)
tags:
  - architecture
  - patterns
  - transactions
---

# 🗄️ Unit of Work (`IUnitOfWork`)

Ubicación: `src/Slotify.Domain/Interfaces/IUnitOfWork.cs`  
Implementación: `src/Slotify.Infrastructure/Data/UnitOfWork.cs`

El patrón **Unit of Work** asegura que múltiples operaciones sobre distintos [[Repositorios]] se confirmen dentro de una **única transacción atómica**.

---

## 📋 Repositorios Agrupados en `IUnitOfWork`
* `IUserRepository Users`
* `IBusinessRepository Businesses`
* `IScheduleBlockRepository ScheduleBlocks`
* `ISectorTemplateRepository SectorTemplates`
* `IAvailabilityScheduleRepository AvailabilitySchedules`
* `IAppointmentRepository Appointments`

---

## ⚡ Método Principal
* `Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);`
  Guarda todos los cambios pendientes en [[AppDbContext]] de forma atómica.

---

## 🔗 Relaciones en el Grafo
- **Contrato en:** [[Modulo Domain]]
- **Implementado en:** [[Modulo Infrastructure]]
- **Consumido por:** [[Modulo Application]] ([[Feature Auth]], [[Feature ScheduleBlocks]])
- **Controla:** [[AppDbContext]], [[Repositorios]]
