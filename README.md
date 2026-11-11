# Slotify - Backend API 🚀

API RESTful desarrollada en **.NET 9** con arquitectura limpia (Clean Architecture / Onion Architecture) y CQRS con MediatR para la plataforma **Slotify**.

---

## 🏗️ Arquitectura del Proyecto

El backend está organizado en las siguientes capas dentro de `src/`:

```
src/
├── Slotify.Domain/         # Entidades, Enums e Interfaces del dominio (Núcleo)
├── Slotify.Application/    # Casos de uso, CQRS (Commands/Queries), DTOs y Validaciones (FluentValidation)
├── Slotify.Infrastructure/ # Acceso a datos (EF Core / PostgreSQL / Supabase), Repositorios, JWT, Hash
└── Slotify.API/            # Controladores REST, Middleware, Configuración de Inyección de Dependencias
```

---

## 🛠️ Tecnologías Utilizadas

- **.NET 9 Web API**
- **Entity Framework Core 8** (PostgreSQL / Supabase)
- **MediatR** (Patrón CQRS)
- **FluentValidation** (Validación de comandos y DTOs)
- **Serilog** (Logging estructurado)
- **JWT (JSON Web Tokens)** (Autenticación y Autorización)
- **BCrypt.Net** (Haseo seguro de contraseñas)

---

## 🚀 Requisitos Previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL o instancia de Supabase
- Editor recomendado: Visual Studio 2022, Visual Studio Code o JetBrains Rider

---

## ⚙️ Configuración y Ejecución

1. **Clonar el repositorio**:
   ```bash
   git clone https://github.com/ShaguittoScot/Slotify_back.git
   cd Slotify_back
   ```

2. **Configurar PostgreSQL y autenticación**

   Usa variables de entorno en el despliegue o un archivo local `.env` excluido de Git:

   ```dotenv
   ConnectionStrings__DefaultConnection="Host=TU_HOST;Port=5432;Database=postgres;Username=TU_USUARIO;Password=TU_PASSWORD;SSL Mode=Require"
   Jwt__Secret="TU_CLAVE_ALEATORIA_DE_32_BYTES_O_MAS"
   Supabase__Url="https://TU_PROYECTO.supabase.co"
   Supabase__AnonKey="TU_CLAVE_ANON_DE_SUPABASE"
   ```

   `Supabase__Url` y `Supabase__AnonKey` se usan al validar la sesión de Supabase. El JWT de la API usa el emisor `Slotify`, la audiencia `Slotify.Api` y una vigencia de 15 minutos.

3. **Preparar el esquema de PostgreSQL**

   Sobre el esquema base, aplica los scripts pendientes de clientes en este orden:

   1. [Migration_US026_ClientesNegocio.sql](Migration_US026_ClientesNegocio.sql) crea `public.clientes_negocio`.
   2. [Migration_US026_ArchivarClientes.sql](Migration_US026_ArchivarClientes.sql) agrega el archivado y la vinculación de citas a la ficha.

   La API no aplica estos scripts al arrancar. El primer script debe ejecutarse una sola vez por base. La agenda usa un bloqueo transaccional de PostgreSQL por negocio y consulta `public.empleados` y `public.usuarios` para resolver el personal.

4. **Restaurar dependencias y compilar**
   ```bash
   dotnet restore
   dotnet build
   ```

5. **Ejecutar el proyecto API**
   ```bash
   dotnet run --project src/Slotify.API
   ```

6. **Consultar OpenAPI**

   En modo desarrollo, abre `/openapi/v1.json` en la dirección que indique la API.

### Prueba de integración con PostgreSQL

Configura `SLOTIFY_TEST_POSTGRES` con la cadena de conexión de una base de pruebas que tenga el esquema de Slotify y ejecuta:

```bash
dotnet test src/Slotify.Tests --filter FullyQualifiedName~CalendarPostgresTests
```

La prueba comprueba el bloqueo transaccional y ejecuta la consulta de personal sin cambiar registros. Si la variable no está configurada, xUnit omite la prueba.

---

## 📱 Repositorio Frontend

El frontend móvil (React Native con Expo) se encuentra en:
👉 [Slotify Frontend Repository](https://github.com/ShaguittoScot/Slotify)
