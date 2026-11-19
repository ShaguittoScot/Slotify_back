---
title: Entidad BusinessBrand
aliases:
  - BusinessBrand
  - Marca de Negocio
  - Entidad BusinessBrand
tags:
  - domain
  - entity
  - brand
  - styling
  - theme
---

# 🎨 Entidad `BusinessBrand`

> [!abstract] Resumen de la Entidad
> Representa la identidad visual, colores corporativos y logotipo oficial de un negocio en Slotify. Mantiene una relación estricta 1:1 con [[Entidad Business]], compartiendo su misma clave primaria (`BusinessId`).

---

## 📍 Metadatos Técnicos
* **Archivo:** `src/Slotify.Domain/Entities/BusinessBrand.cs`
* **Namespace:** `Slotify.Domain.Entities`
* **Herencia:** Clase pura de dominio (su clave primaria coincide con la FK del negocio).
* **Tabla física en PostgreSQL:** `"marca_negocio"`
* **Configuración EF Core:** `src/Slotify.Infrastructure/Data/Configurations/BusinessBrandConfiguration.cs`
* **DbSet:** `AppDbContext.BusinessBrands`

---

## 🔑 Clave Primaria y Foránea
* **`BusinessId` (`Guid`):** Identificador del negocio propietario (`id_negocio`). Actúa concurrentemente como Clave Primaria (PK) y Clave Foránea (FK) hacia `negocios.id`.

---

## 📋 Mapeo de Propiedades C# vs Columnas de Base de Datos

| Propiedad C# | Tipo C# | Columna PostgreSQL | Tipo SQL | Nulo | Valor por Defecto / Configuración |
| :--- | :--- | :--- | :--- | :---: | :--- |
| `BusinessId` | `Guid` | `id_negocio` | `uuid` | No | PK y FK hacia `negocios.id` |
| `LogoUrl` | `string?` | `url_logotipo` | `text` | Sí | URL HTTP/HTTPS o Data URI Base64 del logotipo |
| `PrimaryColor` | `string?` | `color_primario` | `varchar(7)` | Sí | Longitud máx: 7. Default: `"#000000"` |
| `IsDefault` | `bool` | `es_predeterminado` | `boolean` | No | `true` (indica si aún no ha sido personalizado) |
| `UpdatedAt` | `DateTime` | `fecha_actualizacion` | `timestamp` | No | `CURRENT_TIMESTAMP` |

---

## 🔗 Relaciones y Claves Foráneas

```mermaid
erDiagram
    negocios ||--|| marca_negocio : "id_negocio (Cascade)"

    marca_negocio {
        uuid id_negocio PK_FK
        text url_logotipo
        varchar color_primario
        boolean es_predeterminado
        timestamp fecha_actualizacion
    }
```

* **Relación 1:1 con [[Entidad Business]] (`negocios`):**
  - **Propiedad de navegación:** `public Business Business { get; set; } = null!;`
  - **FK física:** `id_negocio`
  - **Configuración Fluent API:**
    ```csharp
    builder.HasOne(x => x.Business)
           .WithOne(x => x.Brand)
           .HasForeignKey<BusinessBrand>(x => x.BusinessId)
           .OnDelete(DeleteBehavior.Cascade);
    ```
  - **Comportamiento en cascada:** `DeleteBehavior.Cascade` (la marca se elimina al borrar el negocio).

---

## 🛡️ Índices y Restricciones
1. **Clave Primaria:** `PK_marca_negocio` en la columna `id_negocio`.
2. **Restricción de Longitud:** `color_primario` restringido a un máximo de 7 caracteres (garantiza el formato `#RRGGBB`).
3. **Clave Foránea:** `FK_marca_negocio_negocios_id_negocio` hacia `negocios(id)` con `ON DELETE CASCADE`.

---

## 💼 Reglas de Negocio Identificadas
1. **Patrón de Tabla de Extensión 1:1:** Permite aligerar las consultas sobre la tabla `negocios` separando los activos pesados de imagen/diseño y evitando columnas nulas innecesarias.
2. **Bandera `IsDefault`:** Indica a las aplicaciones cliente si deben aplicar el tema neutral por defecto de Slotify o si deben inyectar la paleta de la marca en botones, headers y componentes visuales.

---

## 🧪 Ejemplo de Registro Ilustrativo

> [!example] Identidad de Marca Personalizada
> ```json
> {
>   "businessId": "b8f2d1e0-3c4a-4a5e-b9f1-7c8d9e0f1a2b",
>   "logoUrl": "https://cdn.slotify.app/logos/barberia-capital.png",
>   "primaryColor": "#E07A5F",
>   "isDefault": false,
>   "updatedAt": "2026-11-17T21:00:00Z"
> }
> ```

---

## 🔗 Enlaces Relacionados
* [[Modulo Domain]]
* [[Entidad Business]]
* [[AppDbContext]]
* [[Modelo de Datos]]
