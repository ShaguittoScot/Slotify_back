---
title: Flujo General de Petición
tags:
  - flujo
  - ciclo-de-vida
  - arquitectura
---

# 🌊 Flujo General de una Petición (End-to-End)

Este documento describe el ciclo de vida completo de un mensaje o petición HTTP desde que sale de la aplicación móvil / web hasta que se procesa en la base de datos y se retorna la respuesta.

---

## 🗺️ Diagrama de Flujo Arquitectónico

```mermaid
graph TD
    Client["📱 Cliente (React Native / Web)"] -->|1. HTTP Request (JSON)| API["🌐 [[Modulo API]]\n(Controllers)"]
    API -->|2. MediatR.Send(Command/Query)| Pipeline["🛡️ [[Pipeline Behaviors]]\n(Validation / Logging)"]
    Pipeline -->|3. Pasa validación| Application["⚙️ [[Modulo Application]]\n(Handler)"]
    Application -->|4. Invoca repositorio| UOW["🗄️ [[Unit of Work]]\n(Slotify.Infrastructure)"]
    UOW -->|5. Consulta / Modifica| EFCore["⚡ [[AppDbContext]]\n(EF Core 9)"]
    EFCore -->|6. SQL Query / TCP| PostgreSQL["☁️ [[Supabase PostgreSQL]]"]
    PostgreSQL -->|7. Resultado SQL| EFCore
    EFCore -->|8. Entidades| Application
    Application -->|9. ResultResponse<DTO>| API
    API -->|10. 200/201/400 JSON| Client
```

---

## 🔍 Paso a Paso del Flujo

1. **Recepción en API:** El controlador correspondiente en [[Modulo API]] recibe los parámetros o el body JSON.
2. **Despacho con MediatR:** Se instancia un Command o Query y se envía al bus de memoria de MediatR.
3. **Validación Automática:** El middleware [[Pipeline Behaviors]] valida las reglas de negocio con FluentValidation. Si falla, corta el flujo y responde inmediatamente con un error 400.
4. **Lógica en el Handler:** El handler en [[Modulo Application]] ejecuta la lógica del caso de uso.
5. **Persistencia con Unit of Work:** El handler utiliza [[Unit of Work]] y los [[Repositorios]] para interactuar con [[AppDbContext]].
6. **Ejecución en Base de Datos:** Entity Framework traduce la operación a SQL y la ejecuta contra [[Supabase PostgreSQL]].
7. **Retorno de Respuesta Unificada:** Se empaqueta la salida en una clase `ResultResponse<T>` con estructura estándar (`data`, `success`, `message`, `errors`).

---

## 🔗 Enlaces a Flujos Específicos
* [[Flujo de Registro y Sync Auth]]
* [[Flujo de Bloqueo de Horarios]]
* [[Flujo de Disponibilidad de Calendario]]
