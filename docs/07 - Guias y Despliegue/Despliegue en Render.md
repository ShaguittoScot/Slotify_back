---
title: Despliegue en Render
tags:
  - devops
  - render
  - docker
  - produccion
---

# 🚀 Despliegue en Render

El backend está preparado para desplegarse como un servicio web en **Render** mediante contenedores **Docker** con **.NET 9**.

---

## 🛠️ Archivos de Configuración en el Proyecto

* **`Dockerfile`**: Compilación multi-etapa (.NET 9 SDK + ASP.NET Core Runtime).
* **`.dockerignore`**: Exclusión de carpetas temporales (`bin/`, `obj/`, etc.).
* **`render.yaml`**: Blueprint para provisionamiento automático.

---

## 🔑 Variables de Entorno Requeridas en Producción

Configura estas variables en el panel de Render:

| Variable | Descripción / Ejemplo |
| :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Cadena de conexión de Supabase PostgreSQL |
| `Jwt__Secret` | Clave secreta para validación de tokens JWT (mín. 32 chars) |
| `Jwt__Issuer` | `Slotify.API` |
| `Jwt__Audience` | `Slotify.Client` |
| `Jwt__ExpirationInMinutes` | `60` |

---

## 🩺 Endpoint de Health Check

Render monitorea la disponibilidad de la aplicación consultando:
```http
GET /health
```
Respuesta esperada:
```json
{ "status": "Healthy", "timestamp": "..." }
```

---

> [[Dashboard|⬅️ Volver al Dashboard]]
