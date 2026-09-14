# Slotify - Backend API 🚀

API RESTful desarrollada en **.NET 8** con arquitectura limpia (Clean Architecture / Onion Architecture) y CQRS con MediatR para la plataforma **Slotify**.

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

- **.NET 8 Web API**
- **Entity Framework Core 8** (PostgreSQL / Supabase)
- **MediatR** (Patrón CQRS)
- **FluentValidation** (Validación de comandos y DTOs)
- **Serilog** (Logging estructurado)
- **JWT (JSON Web Tokens)** (Autenticación y Autorización)
- **BCrypt.Net** (Haseo seguro de contraseñas)

---

## 🚀 Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL o instancia de Supabase
- Editor recomendado: Visual Studio 2022, Visual Studio Code o JetBrains Rider

---

## ⚙️ Configuración y Ejecución

1. **Clonar el repositorio**:
   ```bash
   git clone https://github.com/ShaguittoScot/Slotify_back.git
   cd Slotify_back
   ```

2. **Configurar variables y conexión**:
   Ajusta la cadena de conexión a PostgreSQL y los parámetros de JWT en `src/Slotify.API/appsettings.json` o usando `user-secrets`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=TU_HOST;Port=5432;Database=postgres;Username=postgres;Password=TU_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
     },
     "Jwt": {
       "Secret": "TU_JWT_SECRET_KEY_MIN_32_CHARS_LONG",
       "Issuer": "Slotify.API",
       "Audience": "Slotify.Client",
       "ExpirationInMinutes": 60
     }
   }
   ```

3. **Restaurar dependencias y compilar**:
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Ejecutar el proyecto API**:
   ```bash
   dotnet run --project src/Slotify.API
   ```

5. **Swagger / OpenAPI**:
   Una vez en ejecución en modo desarrollo, accede a la documentación interactiva en:
   `https://localhost:7001/swagger` (o el puerto configurado).

---

## 📱 Repositorio Frontend

El frontend móvil (React Native con Expo) se encuentra en:
👉 [Slotify Frontend Repository](https://github.com/ShaguittoScot/Slotify)
