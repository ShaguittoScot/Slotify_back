---
title: Flujo de Registro y Sincronización Auth
tags:
  - flujo
  - auth
  - supabase
  - usuarios
---

# 🔐 Flujo de Registro y Sincronización Auth

Este flujo describe cómo un usuario dueño de negocio se registra en el sistema, conectando la autenticación de **Supabase Auth** con el modelo relacional de **Slotify**.

---

## 🔄 Diagrama de Secuencia

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 👤 Usuario / Dueño
    participant App as 📱 App Móvil / Frontend
    participant SupaAuth as ☁️ Supabase Auth
    participant AuthCtrl as 🌐 AuthController
    participant Handler as ⚙️ RegisterAdminHandler
    participant UOW as 🗄️ Unit of Work
    participant DB as 🗄️ Supabase PostgreSQL

    Admin->>App: Ingresa correo y contraseña
    App->>SupaAuth: SignUp(email, password)
    SupaAuth-->>App: Retorna Supabase User ID (UUID)
    
    Note over App,AuthCtrl: Sincronización con Backend Slotify
    App->>AuthCtrl: POST /api/auth/sync { email, name, businessName, sectorTemplateId }
    AuthCtrl->>Handler: MediatR.Send(RegisterAdminCommand)
    
    Handler->>UOW: Users.GetByEmailAsync(email)
    alt Usuario ya existe
        Handler-->>AuthCtrl: Error 409 Conflict
        AuthCtrl-->>App: 409 Conflict
    else Usuario nuevo
        Handler->>UOW: Crear [[Entidad User]] (Rol DUENO)
        Handler->>UOW: Crear [[Entidad Business]] asociada
        Handler->>UOW: SaveChangesAsync()
        UOW->>DB: INSERT INTO "Users", INSERT INTO "Businesses"
        DB-->>UOW: Confirmado
        Handler-->>AuthCtrl: Retorna AuthUserDto
        AuthCtrl-->>App: 200 OK { id, email, businessId }
    end
```

---

## 🧩 Componentes que Intervienen
* **Controlador:** [[Modulo API]] (`AuthController.cs`)
* **Comando y Handler:** [[Feature Auth]] (`RegisterAdminCommand`, `RegisterAdminHandler`)
* **Entidades:** [[Entidad User]], [[Entidad Business]], [[Entidad SectorTemplate]]
* **Persistencia:** [[Unit of Work]], [[AppDbContext]], [[Supabase PostgreSQL]]
