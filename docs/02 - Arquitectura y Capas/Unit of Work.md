---
title: Unit of Work (Patrón de Unidad de Trabajo)
tags:
  - architecture
  - patterns
  - transactions
  - unit-of-work
  - efcore
---

# 🗄️ Unit of Work (`IUnitOfWork`)

* **Contrato (Dominio):** [IUnitOfWork.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Domain/Interfaces/IUnitOfWork.cs)
* **Implementación (Infraestructura):** [UnitOfWork.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Infrastructure/Data/UnitOfWork.cs)

El patrón **Unit of Work** en Slotify tiene la responsabilidad única de coordinar la confirmación atómica y transaccional de los cambios pendientes en la base de datos a través de Entity Framework Core.

---

## 📐 Contrato Real en C#

A diferencia de implementaciones monolíticas tradicionales, la interfaz `IUnitOfWork` en Slotify sigue estrictamente el principio de responsabilidad única (SRP) y **no expone propiedades para los repositorios**:

```csharp
namespace Slotify.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing transactions.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### Implementación en Infraestructura:
```csharp
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly AppDbContext _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

---

## 💉 Inyección de Dependencias Desacoplada

En la arquitectura de Slotify, los repositorios **no se acceden a través de `UnitOfWork`**. En su lugar, cada repositorio específico se inyecta de manera independiente mediante el contenedor de dependencias (DI) directamente en el Handler que lo requiere:

```
                    ┌───────────────────────────┐
                    │  CQRS Handler (MediatR)   │
                    └─────────────┬─────────────┘
                                  │ Inyección por Constructor
            ┌─────────────────────┼─────────────────────┐
            ▼                     ▼                     ▼
┌───────────────────────┐ ┌───────────────┐ ┌───────────────────────┐
│  IClientRepository    │ │  IUnitOfWork  │ │  IBusinessRepository  │
└───────────┬───────────┘ └───────┬───────┘ └───────────┬───────────┘
            │                     │                     │
            └─────────────────────┼─────────────────────┘
                                  ▼
                    ┌───────────────────────────┐
                    │       AppDbContext        │
                    │   (EF Core 9 / Npgsql)    │
                    └───────────────────────────┘
```

Por ejemplo, [RegisterClientHandler.cs](file:///D:/Aplicaciones/Slotify_back/src/Slotify.Application/Features/Auth/Commands/RegisterClientHandler.cs) recibe:
```csharp
public class RegisterClientHandler(
    IClientRepository clientRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterClientCommand, Result<ClientDto>>
```

Esto proporciona ventajas clave:
1. **Segregación de Interfaces (ISP):** Un Handler solo depende de los repositorios que realmente utiliza, sin acoplarse al universo entero de tablas del sistema.
2. **Facilidad de Mocking:** En pruebas unitarias, se puede simular únicamente el repositorio involucrado y el `IUnitOfWork` sin necesidad de instanciar o mockear decenas de propiedades irrelevantes.
3. **Persistencia Transaccional:** Todos los repositorios inyectados en el mismo ámbito de petición (*Scoped*) comparten la misma instancia de `AppDbContext`, por lo que una única invocación a `_unitOfWork.SaveChangesAsync(ct)` confirma todas las operaciones de forma atómica.

---

## 🔗 Relaciones Arquitectónicas
- **Contrato:** [[Modulo Domain]]
- **Implementación:** [[Modulo Infrastructure]]
- **Contexto EF Core:** [[AppDbContext]]
- **Consumo:** [[Modulo Application]] ([[Feature Auth]], [[Feature ScheduleBlocks]], [[Feature Calendar]])
- **Repositorios Asociados:** [[Repositorios]]
