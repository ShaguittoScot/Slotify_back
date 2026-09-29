---
title: Flujo de Registro y Sincronizacion de Cliente Consumidor
tags:
  - flujo
  - auth
  - cliente
  - consumidor
  - onboarding
  - supabase
---

# 👤 Flujo de Registro y Sincronización de Cliente Consumidor (B2C)

Este documento detalla el ciclo de vida completo del registro y sincronización de un **Cliente Consumidor Final** (CLIENTE) entre la aplicación móvil/web (**React Native / Expo**), el servicio de autenticación (**Supabase Auth**) y la base de datos relacional de **Slotify (.NET 8 Web API / PostgreSQL)**, incluyendo la experiencia de **Micro-Onboarding**.

---

## 🧭 Historia de Usuario: US-001 (Cliente Final)

* **Como:** Cliente consumidor que desea agendar citas en negocios.
* **Quiero:** Crear mi cuenta con nombre, correo, teléfono y contraseña, y conocer los beneficios de la plataforma mediante un micro-onboarding ágil.
* **Para:** Descubrir negocios, reservar servicios 24/7 y gestionar mis citas sin llamadas.

---

## 📊 Diagrama de Secuencia End-to-End

`mermaid
sequenceDiagram
    autonumber
    actor Cliente as 👤 Cliente Consumidor
    participant App as 📱 Frontend (RegisterClientScreen)
    participant SupaAuth as 🔐 Supabase Auth
    participant Onboarding as 🚀 ConsumerOnboardingScreen
    participant API as 🌐 AuthController (.NET 8)
    participant MediatR as ⚙️ RegisterClientHandler
    participant ClientRepo as 🗄️ ClientRepository
    participant DB as 🐘 Supabase PostgreSQL (clientes)

    Cliente->>App: Ingresa datos (Nombre, Correo, Teléfono, Contraseña)
    Cliente->>App: Presiona Crear Cuenta
    App->>SupaAuth: signUp(email, password, { role: 'CLIENTE', first_name, last_name, phone })
    SupaAuth-->>App: Retorna Session + User ID (UUID)

    Note over App,API: Sincronización asíncrona con el Backend Slotify
    App->>API: POST /api/auth/sync-client { id, firstName, lastName, email, phone }
    API->>MediatR: Send(RegisterClientCommand)
    MediatR->>ClientRepo: GetBySupabaseIdAsync(id) / EmailExistsAsync(email)

    alt Cliente ya registrado con ese correo/ID
        ClientRepo-->>MediatR: Retorna cliente existente / vincula SupabaseId
        MediatR-->>API: Result<ClientDto>.Ok(ClientDto)
    else Cliente nuevo
        MediatR->>ClientRepo: AddAsync(Client entity)
        MediatR->>DB: INSERT INTO clientes (id, supabase_id, nombre, apellido, email, telefono)
        DB-->>ClientRepo: Registrado con éxito
        MediatR-->>API: Result<ClientDto>.Ok(ClientDto)
    end
    API-->>App: 200 OK (ClientDto)

    Note over App,Onboarding: Transición a Experiencia de Onboarding
    App->>Onboarding: Navega a /(onboarding)/consumer
    Onboarding->>Cliente: Muestra 3 Slides de Valor + Selección de Intereses
    Cliente->>Onboarding: Selecciona preferencias (Barbería, Spa, etc.) o presiona Omitir
    Onboarding->>App: Guarda estado en AsyncStorage (consumerOnboardingComplete)
    Onboarding->>App: Redirige al Explorador /(main)/explore
`

---

## 🧩 Componentes y Capas Intervinientes

### 1. Frontend (Slotify)
* **Pantalla de Registro:** src/features/auth/ui/RegisterClientScreen.tsx
* **Micro-Onboarding:** src/features/consumer-onboarding/ui/ConsumerOnboardingScreen.tsx
* **Capa API:** src/features/auth/api/index.ts (uthApi.registerClient)
* **Endpoint invocado:** POST /api/auth/sync-client

### 2. Backend API (Slotify.API)
* **Controlador:** AuthController.cs
* **Endpoint:** [HttpPost(sync-client)]
* **Firma:** Task<IActionResult> SyncClient([FromBody] RegisterClientCommand command)

### 3. Capa de Aplicación (Slotify.Application)
* **Comando:** RegisterClientCommand.cs
  * Id (Guid de Supabase)
  * FirstName (string)
  * LastName (string)
  * FullName (string opcional)
  * Email (string)
  * Phone (string opcional)
* **Handler:** RegisterClientHandler.cs (orquesta validación de duplicados y persistencia)
* **Validador:** RegisterClientValidator.cs (FluentValidation)
* **DTO:** ClientDto.cs

### 4. Capa de Dominio e Infraestructura (Slotify.Domain / Slotify.Infrastructure)
* **Entidad:** Client.cs (Id, SupabaseId, FirstName, LastName, Email, Phone)
* **Interfaz:** IClientRepository.cs
* **Repositorio:** ClientRepository.cs
* **Mapeo EF Core:** ClientConfiguration.cs -> Tabla clientes en PostgreSQL.

---

## 🛡️ Principios de Seguridad y Aislamiento (Zero-Trust)

1. **Sin dependencia de usiness_id:** A diferencia de los dueños o empleados, los clientes no pertenecen a un inquilino único; pueden agendar en múltiples negocios de la plataforma.
2. **Idempotencia en Sincronización:** Si el cliente ya existe por correo o SupabaseId, el handler devuelve el registro sin fallar ni duplicar filas.
3. **Persistencia Progresiva:** Las preferencias de categorías del onboarding se almacenan en el cliente móvil (AsyncStorage) para optimizar el feed inicial sin sobrecargar la base de datos relacional.

---

## 🔗 Enlaces Relacionados
* [[Flujo de Registro y Sync Auth]]
* [[Flujo General de Peticion]]
* [[Feature Auth]]
* [[Base de datos - Proyecto]]
