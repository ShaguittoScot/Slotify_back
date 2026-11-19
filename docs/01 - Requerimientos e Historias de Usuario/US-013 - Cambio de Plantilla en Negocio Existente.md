---
title: US-013 - Cambio de Plantilla en Negocio Existente
aliases:
  - US-013
  - Cambio de Giro Comercial
tags:
  - historia-usuario
  - negocios
  - plantillas
  - migracion
  - us-013
estado_documental: "Reconstruida desde implementación"
implementacion_backend: "Completa"
integracion_frontend: "Discrepante"
---

# 🔄 US-013: Cambio de Plantilla en Negocio Existente

> [!warning] Reconstruida a partir del código. Pendiente de validación funcional.
> Esta historia de usuario no contaba con una nota independiente en los requerimientos originales de producto. Se formaliza a partir del comando `ChangeBusinessTemplateCommand`, el endpoint `PUT /api/businesses/{id}/template` en `BusinessesController.cs` y su anotación explícita de trazabilidad en código.

* **Como:** Dueño / Administrador de un negocio ya registrado en Slotify.
* **Quiero:** Modificar o migrar la plantilla de sector comercial asignada a mi negocio (por ejemplo, cambiar de "Barbería" a "Spa").
* **Para:** Adaptar el giro comercial de mi establecimiento, incorporar nuevos módulos y resembrar los servicios sugeridos sin tener que crear una cuenta nueva.

---

## 🎯 Criterios de Aceptación (Gherkin)

### Escenario 1: Cambio exitoso de plantilla de sector
```gherkin
Dado que el usuario autenticado es dueño del negocio con identificador {id}
Cuando envía PUT /api/businesses/{id}/template con el cuerpo { "sectorTemplateId": 3 }
Entonces el backend valida que el {id} corresponda al claim businessId del token JWT (o retorna 403 Forbidden)
Y comprueba que la plantilla con id 3 exista en la tabla plantillas_sector (o retorna 404 Not Found)
Y actualiza el campo id_plantilla_sector en la tabla negocios
Y siembra opcionalmente los nuevos servicios sugeridos en la tabla servicios
Y retorna código HTTP 200 OK con el mensaje de confirmación y el estado actualizado.
```

### Escenario 2: Intento de modificación sobre otro negocio (Violación Multi-Tenant)
```gherkin
Dado que un usuario autenticado envía un {id} que no coincide con su businessId
Cuando ejecuta la solicitud PUT /api/businesses/{id}/template
Entonces el backend detiene la ejecución inmediatamente
Y responde con código HTTP 403 Forbidden.
```

---

## ⚙️ Implementación Backend Real

* **Controlador:** `BusinessesController.cs`
* **Ruta HTTP:** `PUT /api/businesses/{id}/template`
* **Comando MediatR:** `ChangeBusinessTemplateCommand` / `ChangeBusinessTemplateHandler`
* **DTO de Solicitud:** `ChangeTemplateRequest` (`{ sectorTemplateId: int }`)
* **Entidades Involucradas:** [[Entidad Business]], [[Entidad SectorTemplate]], [[Entidad Service]]
* **Tablas de Base de Datos Impactadas:** `negocios`, `plantillas_sector`, `servicios`

---

## 📱 Estado de Integración con el Frontend

* **Nivel de Integración:** Discrepante.
* **Discrepancia Documentada:**
  - En la interfaz del frontend existe un método en clientes API que despacha:
    `PATCH /businesses/{id}/sector` con el payload `{ sector: string }`.
  - El backend implementa formalmente:
    `PUT /api/businesses/{id}/template` esperando `{ sectorTemplateId: number }`.
  - Se requiere actualizar el cliente frontend para invocar el método HTTP `PUT` y enviar el identificador numérico de la plantilla.

---

## 🧪 Pruebas que la Respaldan

* **Pruebas de Integración:** `ProtectedEndpointsTests.cs` (verificación de 403 Forbidden ante identificador ajeno y 200 OK en modificación válida).
* **Pruebas de Repositorio:** `BusinessRepositoryTests.cs`.

---

## 🔗 Enlaces Relacionados

* [[Indice de Historias de Usuario]]
* [[US-002 - Plantillas por Sector y Configuracion de Negocio]]
* [[US-006 - Selector Inicial de Giro Comercial]]
* [[US-022 - Aislamiento Multi-Inquilino por Token JWT]]
* [[Feature Businesses]]
* [[Modulo API]]
* [[Contratos de API REST]]
* [[Entidad Business]]
* [[Entidad SectorTemplate]]
