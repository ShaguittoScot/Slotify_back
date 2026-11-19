---
title: Flujo de Registro y Sincronización Auth
tags:
  - flujo
  - auth
  - supabase
  - usuarios
  - negocios
---

# 🔐 Flujo de Registro y Sincronización Auth

Este flujo describe cómo un usuario dueño de negocio se registra en el sistema, conectando la autenticación de **Supabase Auth** con el modelo relacional de **Slotify (.NET 9 / PostgreSQL)**.

---

## 🔄 Diagrama de Secuencia

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 👤 Usuario / Dueño
    participant App as 📱 App Móvil / Frontend
    participant SupaAuth as ☁️ Supabase Auth
    participant AuthCtrl as 🌐 AuthController
    participant Pipeline as 🛡️ ValidationBehavior
    participant Handler as ⚙️ RegisterAdminHandler
    participant UOW as 🗄️ Unit of Work
    participant DB as 🐘 Supabase PostgreSQL (usuarios, negocios)

    Admin->>App: Ingresa correo y contraseña
    App->>SupaAuth: signUp(email, password, metadata)
    SupaAuth-->>App: Retorna Supabase User ID (UUID)
    
    Note over App,AuthCtrl: Sincronización con Backend Slotify
    App->>AuthCtrl: POST /api/auth/sync { id, firstName, lastName, fullName, email, businessName, businessPhone, sectorTemplateId }
    AuthCtrl->>Pipeline: MediatR.Send(RegisterAdminCommand)
    Pipeline->>Handler: Validación exitosa (RegisterAdminValidator)
    
    Handler->>UOW: Users.GetByEmailAsync(email)
    alt Usuario ya existe
        Handler-->>AuthCtrl: Error 409 Conflict
        AuthCtrl-->>App: 409 Conflict { success: false, message: "El correo ya está registrado" }
    else Usuario nuevo
        Handler->>UOW: Crear [[Entidad User]] (Rol DUENO)
        Handler->>UOW: Crear [[Entidad Business]] (Slug generado)
        Handler->>UOW: SaveChangesAsync()
        UOW->>DB: INSERT INTO usuarios, INSERT INTO negocios
        DB-->>UOW: Confirmado
        Handler-->>AuthCtrl: Retorna Result<AuthUserDto> con AccessToken
        AuthCtrl-->>App: 200 OK { success: true, data: AuthUserDto }
    end
```

---

## 🧩 Componentes que Intervienen
* **Controlador:** [[Modulo API]] (`AuthController.cs`)
* **Comando y Handler:** [[Feature Auth]] (`RegisterAdminCommand`, `RegisterAdminHandler`)
* **Validación:** [[Pipeline Behaviors]] (`RegisterAdminValidator`)
* **Entidades:** [[Entidad User]], [[Entidad Business]], [[Entidad SectorTemplate]]
* **Persistencia:** [[06 - Acceso a Datos y Persistencia/Unit of Work|Unit of Work]], [[AppDbContext]], [[Supabase PostgreSQL]]
* **Contratos:** [[Contratos de API REST]]
