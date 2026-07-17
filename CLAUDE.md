# CLAUDE.md — Proyecto UMBRAL v2

> Contexto completo del proyecto para Claude Code y Claude Desktop.
> Cubre arquitectura, las 47 HUs con criterios de aceptación, modelo de dominio para LucidChart, patrones, infraestructura y estado actual.

---

## Identidad del Proyecto

**UMBRAL** es una plataforma web para operar en tiempo real experiencias de investigación inmersiva tipo "escape room académico". Desarrollado para la **UCAB**, materia Desarrollo de Software.

- **Equipo:** Eros Dos Ramos (C.I. 30.371.156) y Adrián Cereijo (C.I. 29.756.926)
- **Versión:** 2.0 — Arquitectura de Microservicios + Keycloak
- **Fecha base del diseño:** Mayo 2026

**Propósito funcional:** Un operador crea sesiones de juego con misiones de tipo Búsqueda del Tesoro o Trivia. Equipos de participantes se unen vía PIN. Todo se ejecuta en tiempo real con WebSockets (SignalR).

---

## Estado Actual (Julio 2026)

### Completado ✅
- **Módulo 1 (HU-01 a HU-06):** Login, registro, gestión de usuarios vía Keycloak (todo alojado en Missions.Service — ver nota de arquitectura abajo)
- **Módulo 2 (HU-07 a HU-14):** CRUD completo de misiones, etapas y pistas
- **Módulo 3 (HU-15 a HU-19):** Equipos como sub-agregado de Sessions.Service (`SessionTeam`), unirse sin código
- **Módulo 4 (HU-20 a HU-27):** Sesiones en vivo con ciclo completo de estados, PIN, monitoreo, pistas
- **Feature multi-mission-session:** Sesiones con múltiples misiones (SessionStage, JSONB) — 248 tests pasando
- **Trivia Service:** CRUD de quizzes/preguntas/respuestas (HU-28 a HU-35, pendiente de merge)
- **CI/CD:** `.github/workflows/ci.yml` — build+test por servicio (Missions, Sessions, Trivia, Gateway+RealTimeHub, Frontend), más job de E2E en push a `develop`/`main`
- **Tests E2E:** `tests/e2e/` con Playwright (`admin-flow.spec.ts`), contra el stack completo vía Docker Compose

### Pendiente 🔲
- **HU-36 a HU-47:** Ejecución de trivia en vivo completa — comandos base existen (`StartTrivia`, `AskQuestion`, `CloseQuestion`, `EndTriviaGame`) pero el flujo end-to-end frontend↔backend no está integrado
- **SignalR frontend:** `useSignalR.ts` existe y el `GameHub` está implementado, pero el cableado con las vistas del participante en vivo (WaitingRoom→ActiveGame) necesita completarse

### Rama actual
- Branch: `feature/bug-fixes-rf-gaps`
- Último commit: `4a23dea fix(sessions): bugs varios de sesion en vivo y mejoras de operador/mision`

---

## Stack Tecnológico

| Componente | Tecnología | Versión |
|------------|-----------|---------|
| **Auth** | Keycloak (OIDC + Authorization Code + PKCE) | 26.1 |
| **Backend** | ASP.NET Core (C#) | .NET 10 |
| **Frontend** | React + TypeScript + Vite | React 19, TS 6.x, Vite 8 |
| **Base de datos** | PostgreSQL (una por microservicio) | 16 Alpine |
| **Tiempo real** | SignalR (WebSockets) | — |
| **Mensajería** | RabbitMQ (topic exchange) | 4-management |
| **API Gateway** | YARP (Yet Another Reverse Proxy) | — |
| **CQRS** | MediatR + FluentValidation | — |
| **ORM** | Entity Framework Core (`EnsureCreated()`) | 10 |
| **Testing** | xUnit + FluentAssertions + NSubstitute | — |
| **Contenedores** | Docker + Docker Compose | — |

---

## Arquitectura del Sistema

> **Nota de arquitectura (corregida Julio 2026):** el diseño original contemplaba un **Teams.Service** independiente (con su propia DB y un VO `JoinCode`). En la implementación real ese servicio **nunca llegó a existir como tal / fue absorbido**: los equipos ahora son un sub-agregado de `Sessions.Service` (`SessionTeam`/`SessionTeamMember`, sin JoinCode — se crean dentro de una sesión ya creada), y toda la gestión de usuarios/operadores/participantes vía Keycloak Admin API quedó dentro de `Missions.Service`. La arquitectura real tiene **3 microservicios de dominio** (no 4), más el Gateway y el RealTimeHub.

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Gateway (:5000)                       │
│                    YARP + Validación JWT Keycloak                │
└──────────────┬──────────────────────┬─────────────────────────┬──┘
               │                      │                          │
    ┌──────────▼──────────┐  ┌────────▼───────────┐   ┌──────────▼───┐
    │   Missions.Service  │  │  Sessions.Service   │   │Trivia.Service│
    │      (:5001)        │  │      (:5002)        │   │   (:5004)    │
    │  missions-db        │  │  sessions-db        │   │  trivia-db   │
    │  ─ misiones/etapas  │  │  ─ ciclo de vida     │   │  ─ quizzes   │
    │  ─ pistas           │  │  ─ SessionTeam(s)    │   │  ─ trivia    │
    │  ─ operadores       │  │  ─ SessionParticipant│   │    en vivo   │
    │  ─ participantes     │  │  ─ auditoría         │   │  ─ ranking   │
    │  ─ Keycloak Admin API│  └─────────────────────┘   └──────────────┘
    └──────────────────────┘                                     │
                                                                │
    ┌────────────────────────────────────┐   ┌─────────────────▼────┐
    │         RabbitMQ (:5672)           │   │   RealTimeHub (:5005) │
    │   exchange: trivia.exchange        │◀──│   GameHub (SignalR)   │
    │   cola: trivia.answer.submitted    │   └──────────────────────┘
    └────────────────────────────────────┘
               │
    ┌──────────▼─────────────────────────────────────────────────────┐
    │                    Keycloak (:8080)                             │
    │    Realm: umbral | Roles: admin, operator, participant         │
    └────────────────────────────────────────────────────────────────┘
               │
    ┌──────────▼──────────────────────────────────────────────────────┐
    │                   Frontend React (:5173)                         │
    │   oidc-client-ts + @microsoft/signalr + react-router-dom       │
    └─────────────────────────────────────────────────────────────────┘
```

### Arquitectura por servicio: Hexagonal / Clean Architecture

```
Domain      → Entidades, Value Objects, Interfaces de repositorio, Enums
Application → Commands, Queries, Handlers, DTOs, Validators (MediatR + CQRS)
Infrastructure → EF Core DbContext, Repositorios, RabbitMQ, Servicios externos
Api         → Controllers, Program.cs, Middleware
```

**Regla fundamental:** Domain no conoce Infrastructure. Application depende de Domain, no de Infrastructure.

---

## Actores del Sistema

| Actor | Rol en Keycloak | Responsabilidades |
|-------|----------------|-------------------|
| **Administrador** | `admin` | Gestiona operadores, misiones, trivias, supervisa la plataforma |
| **Operador** | `operator` | Crea sesiones, gestiona equipos, libera pistas, opera en vivo |
| **Participante** | `participant` | Se registra, se une a sesiones, responde trivias, consulta su progreso |

---

## ERS Completo — Las 47 Historias de Usuario

### MÓDULO 1: Accesos y Gestión de Usuarios (Keycloak)

#### HU-01: Iniciar sesión en la plataforma
**Actor:** Usuario (Admin, Operador o Participante)  
**Prioridad:** Alta  
**Historia:** Como Usuario, quiero iniciar sesión con mis credenciales a través de Keycloak, para acceder a la plataforma según mi rol.

**Criterios de Aceptación:**
1. El frontend redirige al formulario de login de Keycloak (OIDC — no hay login propio).
2. Keycloak valida las credenciales contra su base de datos.
3. Keycloak emite un JWT (access token + refresh token) con los roles del usuario.
4. El frontend almacena el token y lo usa para llamar a los microservicios.
5. El API Gateway valida el token JWT en cada request antes de enrutar.
6. Usuarios inactivos/deshabilitados en Keycloak no pueden autenticarse.
7. Soporte SSO: una sesión funciona en todas las vistas.
8. El usuario puede cerrar sesión desde la interfaz (limpia token local + logout Keycloak).
9. Después del login, el usuario es redirigido al dashboard según su rol.
10. Keycloak es el único proveedor de autenticación del sistema.

**Flujo OIDC:**
```
Frontend → Keycloak: GET /auth/realms/umbral/protocol/openid-connect/auth
Keycloak → Frontend: Authorization code
Frontend → Keycloak: POST /token (code + PKCE)
Keycloak → Frontend: Access Token + Refresh Token + ID Token
Frontend → API Gateway: Bearer access_token en cada request
API Gateway → Keycloak: Validación JWKS (cacheada, sin llamada por request)
API Gateway → Microservicio: Request interno (confianza de red)
```

---

#### HU-02: Crear cuenta de Operador
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero crear una cuenta de Operador en Keycloak, para asignar personal encargado de ejecutar las sesiones.

**Criterios de Aceptación:**
1. El Administrador accede a un formulario con: nombre, email, contraseña.
2. `POST /api/admin/operators` (Missions.Service) llama a la Admin API de Keycloak para crear el usuario.
3. El usuario se crea en Keycloak con rol `operator` y atributo personalizado `name`.
4. El email debe ser único en Keycloak.
5. La cuenta se crea habilitada (`enabled = true`).
6. Se retorna confirmación al frontend con HTTP 201.

---

#### HU-03: Desactivar cuenta de Operador
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero deshabilitar la cuenta de un Operador en Keycloak, para revocar su acceso.

**Criterios de Aceptación:**
1. El Administrador puede deshabilitar un operador desde el panel de usuarios.
2. Se llama a la Admin API de Keycloak para setear `enabled = false`.
3. Keycloak invalida todos los tokens activos del usuario (revocación).
4. El operador deshabilitado no puede iniciar sesión.
5. El sistema solicita confirmación antes de ejecutar.

---

#### HU-04: Registrar cuenta de Participante
**Actor:** Participante  
**Prioridad:** Alta  
**Historia:** Como Participante, quiero registrarme en la plataforma a través de Keycloak, para unirme a sesiones y que mis puntajes queden guardados.

**Criterios de Aceptación:**
1. El frontend ofrece formulario de registro con: nombre, alias, email, contraseña.
2. El registro se procesa a través de la API Admin REST de Keycloak.
3. El usuario se crea con rol `participant` y atributo personalizado `alias`.
4. Alias y email deben ser únicos.
5. Tras el registro exitoso, se inicia sesión automáticamente (SSO).

---

#### HU-05: Modificar perfil de Participante
**Actor:** Participante  
**Prioridad:** Media  
**Historia:** Como Participante, quiero editar los datos de mi perfil (nombre, alias), para mantener mi información actualizada.

**Criterios de Aceptación:**
1. El participante accede a una sección "Mi Perfil".
2. Puede editar nombre y alias.
3. Para cambiar contraseña, debe ingresar la actual primero.
4. Los cambios se sincronizan con Keycloak vía Admin API (atributos personalizados).
5. El alias debe ser único entre todos los participantes.

---

#### HU-06: Consultar listado y perfiles de usuarios
**Actor:** Administrador u Operador  
**Prioridad:** Media  
**Historia:** Como Administrador u Operador, quiero visualizar un listado de usuarios registrados, para consultar sus perfiles.

**Criterios de Aceptación:**
1. Listado paginado con nombre, email, rol y estado (habilitado/deshabilitado).
2. Filtros por rol (`admin`, `operator`, `participant`) y estado.
3. Búsqueda por nombre o email.
4. Detalle completo del usuario al seleccionarlo.
5. No se exponen contraseñas ni datos sensibles.

---

### MÓDULO 2: Gestión de Misiones

#### HU-07: Crear misión
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero crear una nueva misión con su narrativa y parámetros básicos, para disponer de un nuevo reto en el catálogo.

**Criterios de Aceptación:**
1. `POST /api/missions` — Formulario con: Título, Descripción, Dificultad (Fácil/Media/Difícil), Tiempo (15/30/60/90 min), Tipo (Tesoro/Trivia).
2. El título debe ser único.
3. La misión se crea en estado "Borrador".
4. Retorna HTTP 201 Created.

---

#### HU-08: Consultar catálogo de misiones
**Actor:** Administrador u Operador  
**Prioridad:** Media  
**Historia:** Como Administrador u Operador, quiero consultar el catálogo de misiones, para ver sus detalles.

**Criterios de Aceptación:**
1. `GET /api/missions` — Listado paginado con título, dificultad, tipo y estado.
2. Filtros por estado y dificultad; búsqueda por nombre.
3. `GET /api/missions/{id}` — Detalle completo con etapas y pistas.
4. `GET /api/missions/active` — Solo misiones activas.
5. Requiere autenticación vía Keycloak.

---

#### HU-09: Modificar configuración de una misión
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero editar los datos generales de una misión existente.

**Criterios de Aceptación:**
1. `PUT /api/missions/{id}` — Editar título, descripción, dificultad y tiempo.
2. Validar unicidad del título si cambia.
3. Bloquear edición si la misión está en uso en una sesión activa.
4. Reflejar cambios inmediatamente.

---

#### HU-10: Activar / Desactivar misión
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero cambiar el estado de una misión (Activa o Inactiva), para habilitar o retirar su uso en sesiones.

**Criterios de Aceptación:**
1. `PATCH /api/missions/{id}/status` — Alternar entre Activa/Inactiva.
2. Solo puede pasar a Activa si tiene al menos una etapa registrada.
3. Bloquear desactivación si está vinculada a una sesión activa.

---

#### HU-11: Agregar etapas a una misión
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero agregar etapas secuenciales a una misión, para estructurar el flujo del juego.

**Criterios de Aceptación:**
1. `POST /api/missions/{id}/stages` — Nombre, descripción, orden secuencial.
2. Validar que no se repita el número de orden.
3. Reflejar inmediatamente en el detalle de la misión.

---

#### HU-12: Modificar etapas de una misión
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero editar la información de las etapas de una misión.

**Criterios de Aceptación:**
1. `PUT /api/missions/{id}/stages/{stageId}` — Editar nombre, descripción y orden.
2. Bloquear si la misión está activa y en uso en sesión.

---

#### HU-13: Agregar pistas a una etapa
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero registrar pistas asociadas a una etapa, para ofrecer ayudas graduales.

**Criterios de Aceptación:**
1. `POST /api/missions/{id}/stages/{stageId}/clues` — Contenido de la pista.
2. Penalización de puntos (opcional).
3. Tipo de liberación: automática (por tiempo) o manual (operador).

---

#### HU-14: Eliminar pistas de una etapa
**Actor:** Administrador  
**Prioridad:** Media  
**Historia:** Como Administrador, quiero eliminar pistas de una etapa.

**Criterios de Aceptación:**
1. `DELETE /api/missions/{id}/stages/{stageId}/clues/{clueId}`.
2. Confirmación previa del usuario.
3. Bloquear si la misión está en uso en sesión activa.

---

### MÓDULO 3: Gestión de Equipos

> **Nota de arquitectura del Módulo 3:** el diseño original de HU-15 a HU-19 asumía un `Teams.Service` independiente con equipos preformados (VO `JoinCode`) antes de existir una sesión. Esa decisión fue reemplazada: los equipos ahora se crean **dentro de** `Sessions.Service`, como sub-agregado de una sesión concreta (`SessionTeam`/`SessionTeamMember`). No hay `JoinCode` — el participante se une directamente a un equipo de la sesión en la que ya está autenticado. Las HUs de abajo describen el requerimiento original y su "Nota de implementación" documenta el comportamiento real.

#### HU-15: Crear equipo participante
**Actor:** Operador o Administrador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero registrar un equipo con un líder y miembros opcionales, para agrupar a los participantes.

**Criterios de Aceptación:**
1. `POST /api/teams` — Nombre, descripción, ID del líder (Participante) y lista opcional de miembros.
2. Nombre del equipo único.
3. El líder debe ser un Participante registrado en Keycloak.
4. Al crear, se genera automáticamente un `JoinCode` alfanumérico de 6 caracteres.
5. Retorna HTTP 201 Created con el JoinCode.

**Nota de implementación:** `POST /api/sessions/{sessionId}/teams` (rol `operator`) — solo `Name` (máx. 100 caracteres, único por sesión). No hay concepto de líder ni `JoinCode`; `MaxMembers` es fijo en 5. Retorna HTTP 201 con el equipo creado.

---

#### HU-16: Consultar listado de equipos
**Actor:** Operador o Administrador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero consultar el listado de equipos registrados.

**Criterios de Aceptación:**
1. `GET /api/teams` — Listado paginado con nombre, descripción, cantidad de miembros y código de unión.
2. Búsqueda por nombre.
3. Requiere autenticación (operador o admin).

**Nota de implementación:** `GET /api/sessions/{sessionId}/teams` (autenticado) — lista los equipos de una sesión concreta, no un catálogo global de equipos.

---

#### HU-17: Ver detalle de equipo
**Actor:** Operador o Administrador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero ver el detalle de un equipo con sus miembros.

**Criterios de Aceptación:**
1. `GET /api/teams/{id}` — Datos del equipo + lista de miembros (IDs de Keycloak).
2. HTTP 404 si el equipo no existe.
3. Requiere autenticación.

**Nota de implementación:** no existe un endpoint de detalle por equipo individual; el detalle (incluyendo miembros) viaja dentro de la respuesta de `GET /api/sessions/{sessionId}/teams`.

---

#### HU-18: Modificar equipo
**Actor:** Operador o Administrador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero modificar los datos de un equipo y gestionar sus miembros.

**Criterios de Aceptación:**
1. `PUT /api/teams/{id}` — Editar nombre y descripción.
2. Agregar o quitar miembros con listas `addMemberIds` y `removeMemberIds`.
3. No se puede agregar un miembro que ya está en el equipo (HTTP 409 Conflict).
4. Retorna HTTP 204 No Content.

**Nota de implementación:** no hay edición de nombre/descripción. La única operación de gestión de miembros implementada es `DELETE /api/sessions/{sessionId}/teams/{teamId}/members/{userId}` (rol `operator`); agregar miembros ocurre vía el join del propio participante (HU-19), no por acción del operador.

---

#### HU-19: Unirse a un equipo con código
**Actor:** Participante  
**Prioridad:** Alta  
**Historia:** Como Participante, quiero unirme a un equipo usando un código compartido por el líder.

**Criterios de Aceptación:**
1. `POST /api/teams/{id}/join` — Body: `{ joinCode }`. Usuario identificado por token JWT (claim `sub`).
2. Código incorrecto → HTTP 403 Forbidden.
3. Ya es miembro → HTTP 409 Conflict.
4. Equipo no existe → HTTP 404 Not Found.
5. Éxito → HTTP 204 No Content.

**Nota de implementación:** `POST /api/sessions/{sessionId}/teams/{teamId}/join` (autenticado, sin código) — el participante se une directamente porque ya está autenticado y (normalmente) ya se unió a la sesión vía PIN (HU-23). Bloquea si el equipo alcanzó `MaxMembers` (5) o si el usuario ya es miembro.

---

### MÓDULO 4: Gestión de Sesiones en Vivo

#### HU-20: Crear una nueva sesión de juego
**Actor:** Operador o Administrador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero crear una nueva sesión en vivo a partir de una o más misiones del catálogo, para generar una sala con múltiples etapas de juego.

**Criterios de Aceptación:**
1. `POST /api/sessions` — Nombre y **array de IDs de misiones** (una o más, en orden de juego).
2. El sistema genera un PIN numérico de 6 dígitos (único, verificación de colisiones).
3. La sesión se crea en estado "Programada" con `CurrentStageOrder = 0`.
4. Las misiones se almacenan como `SessionStage[]` (Value Objects en JSONB) con su orden, título y tipo.
5. Retorna HTTP 201 Created con el PIN y el ID de sesión.
6. Se publica un evento en RabbitMQ notificando la creación.

**Nota de implementación:** El modelo de sesión multi-misión (`SessionStage` como JSONB) fue implementado en `feature/multi-mission-session`. Un operador puede avanzar de etapa con `POST /{sessionId}/stages/{stageId}/advance`.

---

#### HU-21: Consultar sesiones de juego
**Actor:** Operador o Administrador  
**Prioridad:** Media  
**Historia:** Como Operador, quiero consultar el listado de sesiones y ver su detalle.

**Criterios de Aceptación:**
1. `GET /api/sessions` — Listado paginado con filtros por misión, estado y fecha.
2. `GET /api/sessions/{id}` — Detalle completo con misión, equipos y etapas.
3. `GET /api/sessions/active` — Sesiones activas o en preparación.

---

#### HU-22: Editar estados de sesión
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero avanzar una sesión por su ciclo de vida.

**Criterios de Aceptación:**
1. `PATCH /api/sessions/{id}/status` — Transiciones válidas (State Pattern):
   - `Scheduled` → `Preparing` | `Cancelled`
   - `Preparing` → `Active` | `Cancelled`
   - `Active` → `Paused` | `Finished` | `Cancelled`
   - `Paused` → `Active` | `Finished` | `Cancelled`
   - `Finished` / `Cancelled` → (terminales, no cambian)
2. Al activar por primera vez, se setea `StartedAt`.
3. Al finalizar, se setea `EndedAt`.
4. WebSockets notifican a todos los clientes conectados (evento `SessionStatusChanged`).
5. La cadena de validación (`BaseMissionStatusHandler → ValidStatusHandler → NotTerminalHandler`) se ejecuta antes de cada transición.

---

#### HU-23: Unirse a una sesión en vivo
**Actor:** Participante  
**Prioridad:** Alta  
**Historia:** Como Participante, quiero ingresar el PIN de una sesión para unirme a la sala.

**Criterios de Aceptación:**
1. `POST /api/sessions/{id}/join` — Body: `{ pin, userId, userAlias }`.
2. Validar que la sesión esté en estado `Preparing`.
3. Error si el PIN no coincide o la sesión ya está `Active`, `Finished` o `Cancelled`.
4. Al unirse, se crea un registro `SessionParticipant` (userId + alias) vinculado a la sesión.
5. WebSockets notifican al operador vía el grupo `session:{sessionId}`.

**Nota de implementación:** Los participantes se unen directamente a la sesión (no a través de un equipo). La entidad es `SessionParticipant { Id, SessionId, UserId, UserAlias, JoinedAt }`.

---

#### HU-24: Iniciar sesión de juego en vivo
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero iniciar la partida desde la sala de espera, para que todos comiencen simultáneamente.

**Criterios de Aceptación:**
1. Botón "Iniciar Partida" en el panel del operador.
2. La sesión cambia de "EnPreparación" a "Activa".
3. Todos los participantes reciben la primera etapa vía WebSockets.
4. El temporizador general comienza a correr.

---

#### HU-25: Monitorear progreso
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero visualizar un panel de control en tiempo real del avance de cada equipo.

**Criterios de Aceptación:**
1. Tablero con todos los equipos, su etapa/pregunta actual y puntaje.
2. Indicador visual del tiempo transcurrido.
3. Actualización en tiempo real vía WebSockets (`ProgressUpdated`).

---

#### HU-26: Liberar pistas manualmente
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero activar pistas específicas para un equipo, para ayudarlos a avanzar.

**Criterios de Aceptación:**
1. Botón "Enviar Pista" junto a cada equipo en el dashboard.
2. Seleccionar qué pista liberar (de las disponibles para la etapa actual).
3. Notificación inmediata al equipo vía WebSockets (`ClueReleased`).
4. Registro automático de la penalización de puntos.

---

#### HU-27: Finalizar sesión de juego y cerrar ranking
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero dar por terminada la sesión, para cerrar el ingreso de puntos y generar el ranking final.

**Criterios de Aceptación:**
1. Opción "Finalizar Sesión" detiene el tiempo.
2. Cálculo automático del puntaje final por equipo.
3. Estado cambia a "Finalizada" (solo desde "Activa" o "Pausada").
4. Pantalla de resultados se muestra a participantes vía WebSockets.
5. Mostrar resumen estadístico al operador.

---

### MÓDULO 5: Gestión de Trivias

#### HU-28: Crear cuestionario de trivia
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero crear un nuevo cuestionario de trivia vacío.

**Criterios de Aceptación:**
1. `POST /api/quizzes` — Título del cuestionario.
2. Se registra con ID único.
3. Retorna HTTP 201 con el ID del quiz creado.

**Nota de implementación:** La entidad `Quiz` tiene `Id: Guid` y `Title: string`. No tiene campo `Description` en la implementación actual.

---

#### HU-29: Eliminar cuestionario de trivia
**Actor:** Administrador  
**Prioridad:** Media  
**Historia:** Como Administrador, quiero eliminar un cuestionario de trivia.

**Criterios de Aceptación:**
1. `DELETE /api/quizzes/{id}` — Confirmación previa.
2. Borrado en cascada de preguntas y respuestas asociadas.
3. Bloquear si está en uso en sesión activa.

---

#### HU-30: Consultar cuestionario de trivia
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero consultar la lista y detalle de los cuestionarios.

**Criterios de Aceptación:**
1. `GET /api/quizzes` — Listado con nombre, descripción y número de preguntas.
2. `GET /api/quizzes/{id}` — Detalle completo con preguntas y respuestas.
3. Búsqueda por nombre.

---

#### HU-31: Agregar pregunta al cuestionario
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero agregar una nueva pregunta a un cuestionario existente.

**Criterios de Aceptación:**
1. `POST /api/quizzes/{id}/questions` — Enunciado (texto), tiempo límite en segundos.
2. Se vincula automáticamente al cuestionario.
3. Actualización inmediata de la vista.

---

#### HU-32: Editar pregunta
**Actor:** Administrador  
**Prioridad:** Media  
**Historia:** Como Administrador, quiero editar el enunciado de una pregunta existente.

**Criterios de Aceptación:**
1. `PUT /api/quizzes/{id}/questions/{questionId}` — Modificar enunciado y tiempo límite.
2. Se conservan las respuestas asociadas.

---

#### HU-33: Eliminar pregunta
**Actor:** Administrador  
**Prioridad:** Media  
**Historia:** Como Administrador, quiero eliminar una pregunta de un cuestionario.

**Criterios de Aceptación:**
1. `DELETE /api/quizzes/{id}/questions/{questionId}`.
2. Confirmación previa.
3. Elimina respuestas asociadas en cascada.

---

#### HU-34: Definir respuestas
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero agregar opciones de respuesta a una pregunta.

**Criterios de Aceptación:**
1. `POST /api/quizzes/{id}/questions/{questionId}/answers` — Texto de la opción.
2. Mínimo 2, máximo 4 opciones por pregunta.
3. Ninguna opción puede estar en blanco.

---

#### HU-35: Marcar respuesta correcta
**Actor:** Administrador  
**Prioridad:** Alta  
**Historia:** Como Administrador, quiero seleccionar cuál opción es la correcta, para que el sistema evalúe automáticamente.

**Criterios de Aceptación:**
1. `PATCH /api/quizzes/{id}/questions/{questionId}/correct` — Marcar una única opción como correcta.
2. Al marcar una nueva, desmarcar automáticamente la anterior.
3. Impedir activar el cuestionario si hay preguntas sin respuesta correcta.

---

### MÓDULO 6: Ejecución de Trivia en Vivo

#### HU-36: Iniciar juego de trivia
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero dar inicio formal a la sesión de trivia.

**Criterios de Aceptación:**
1. `POST /start-trivia` — Body: `{ quizId, sessionId }`.
2. Validar que la sesión esté en `Preparing`.
3. Cambiar estado a `Active` y registrar inicio en `QuizSession`.
4. Habilitar canal WebSockets para transmisión sincrónica.

**Nota de implementación:** `StartTriviaCommand` y `StartTriviaCommandHandler` ya existen en Trivia.Service.

---

#### HU-37: Finalizar juego de trivia
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero finalizar el juego de trivia para congelar puntuaciones.

**Criterios de Aceptación:**
1. `POST /games/{sessionId}/end` — EndTriviaGame.
2. Finalización automática si se responde la última pregunta.
3. Solo desde `Active` o `Paused`.
4. Pausar y reanudar disponibles.
5. Guardar puntaje histórico final en `LeaderboardEntry`.

**Nota de implementación:** `EndTriviaGameCommand` ya existe en Trivia.Service.

---

#### HU-38: Mostrar pregunta en tiempo real
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero enviar la pregunta actual a todos los equipos simultáneamente.

**Criterios de Aceptación:**
1. `POST /questions/ask` — Body: `{ quizId, sessionId, questionId }`.
2. El sistema emite evento `QuestionAsked` vía WebSockets a todos en `session:{sessionId}`.
3. Los equipos renderizan la pregunta de forma fluida con el temporizador.
4. Reconexión automática si se pierde la señal WebSocket.

**Nota de implementación:** `AskQuestionCommand`, `AskQuestionCommandHandler` y el endpoint `POST /internal/notifications/question-asked` en RealTimeHub ya existen.

---

#### HU-39: Mostrar contador regresivo por pregunta
**Actor:** Participante  
**Prioridad:** Alta  
**Historia:** Como Participante, quiero ver un temporizador en cuenta regresiva, para saber cuánto tiempo tengo para responder.

**Criterios de Aceptación:**
1. Iniciar cuenta regresiva al renderizar la pregunta.
2. El tiempo corresponde al configurado para esa pregunta (`TimeLimitSeconds`).
3. Al llegar a cero, deshabilitar botones de respuesta automáticamente.

---

#### HU-40: Enviar respuesta de trivia
**Actor:** Participante  
**Prioridad:** Alta  
**Historia:** Como Participante, quiero seleccionar una opción y enviar mi respuesta antes de que el contador llegue a cero.

**Criterios de Aceptación:**
1. `POST /answers` — Body: `{ quizId, teamId, teamName, questionId, answerId, timestamp }`.
2. Bloquear la interfaz inmediatamente después de seleccionar.
3. El servidor evalúa la respuesta (`IsCorrect`) y calcula puntaje con `TimeBasedScoringStrategy`.
4. Se publica `TriviaAnswerSubmittedEvent` a RabbitMQ (routing key `answer.submitted`).
5. Confirmación visual de recepción con indicador de correcto/incorrecto.

**Nota de implementación:** `SubmitAnswerCommand` ya existe con `TimeBasedScoringStrategy`. El `TriviaAnswerSubmittedConsumer` consume de la cola `trivia.answer.submitted`, actualiza `LeaderboardEntry` y publica `LeaderboardUpdated`.

---

#### HU-41: Mostrar ranking en tiempo real de trivia
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero proyectar la tabla de posiciones actualizada después de cada pregunta.

**Criterios de Aceptación:**
1. Calcular puntajes acumulados inmediatamente después de cerrar cada pregunta.
2. Ordenar de mayor a menor puntaje (tiempo como desempate).
3. Transmitir ranking actualizado vía WebSockets (evento `RankingUpdated`).
4. El recálculo se procesa de forma asíncrona vía RabbitMQ (`TriviaAnswerSubmittedConsumer`).

**Nota de implementación:** El endpoint `POST /internal/events/LeaderboardUpdated` en RealTimeHub emite el evento `RankingUpdated` a todos los conectados en la sesión. `GET /ranking` retorna el leaderboard actual.

---

#### HU-42: Actualizar ranking después de cada pregunta
**Actor:** Sistema  
**Prioridad:** Alta  
**Historia:** Como Sistema, quiero recalcular la tabla de posiciones inmediatamente después de cerrar cada pregunta.

**Criterios de Aceptación:**
1. Al recibir `TriviaAnswerSubmittedEvent` de RabbitMQ, actualizar `LeaderboardEntry` del equipo.
2. Publicar `LeaderboardUpdated` al RealTimeHub para broadcast vía WebSockets.
3. WebSockets actualizan automáticamente sin recargar la página.

**Nota de implementación:** Flujo completo: `SubmitAnswer` → RabbitMQ → `TriviaAnswerSubmittedConsumer` → `UpdateLeaderboardCommand` → `POST /internal/events/LeaderboardUpdated` → SignalR `RankingUpdated`.

---

#### HU-43: Mostrar resultados de cada pregunta
**Actor:** Operador  
**Prioridad:** Media  
**Historia:** Como Operador, quiero ver el desglose de respuestas por opción, para analizar el desempeño del grupo.

**Criterios de Aceptación:**
1. Al terminar la ronda, calcular cantidad y porcentaje de respuestas por opción.
2. Mostrar gráficamente en la pantalla del operador.

---

#### HU-44: Mostrar respuesta correcta después de cada ronda
**Actor:** Participante  
**Prioridad:** Alta  
**Historia:** Como Participante, quiero ver la respuesta correcta al finalizar la ronda para verificar si acerté.

**Criterios de Aceptación:**
1. Al cerrar la ronda, resaltar la respuesta correcta en la pantalla del participante.
2. Indicar si la elección del equipo fue un acierto o error.

---

### MÓDULO 7: Visualización de Progreso y Resultados

#### HU-45: Consultar resultados finales del juego
**Actor:** Operador  
**Prioridad:** Alta  
**Historia:** Como Operador, quiero acceder a una pantalla de cierre con el podio final.

**Criterios de Aceptación:**
1. Mostrar los 3 primeros lugares destacados (podio).
2. Listado completo de todos los equipos y puntajes.
3. Bloquear alteración de resultados.

---

#### HU-46: Consultar resultados individuales post-partida
**Actor:** Participante  
**Prioridad:** Alta  
**Historia:** Como Participante, quiero visualizar mi resumen de desempeño al finalizar la sesión.

**Criterios de Aceptación:**
1. Pantalla de resultados al finalizar la sesión.
2. Mostrar: puntaje total, tiempo, pistas usadas, posición en el ranking.
3. Opción "Volver al Inicio".
4. Los datos deben coincidir con el panel del operador.

---

#### HU-47: Visualizar Ranking Global
**Actor:** Participante  
**Prioridad:** Media  
**Historia:** Como Participante, quiero ver una tabla de posiciones global, para comparar mi rendimiento histórico.

**Criterios de Aceptación:**
1. Listado público de mejores puntajes acumulados.
2. Mostrar: alias, puntos totales, misiones completadas.
3. Filtro "Todos los tiempos" o "Mensual".
4. Resaltar la posición propia del usuario autenticado.

---

## Requerimientos No Funcionales

| Código | Descripción |
|--------|-------------|
| RNF-01 | Frontend React 19 + Microservicios .NET 10 |
| RNF-02 | Persistencia PostgreSQL 16 por servicio |
| RNF-03 | WebSockets (SignalR) para tiempo real |
| RNF-04 | CQRS con MediatR en cada microservicio |
| RNF-05 | RabbitMQ para procesos asíncronos |
| RNF-06 | Arquitectura Hexagonal en cada microservicio |
| RNF-07 | Domain sin dependencias de Infrastructure |
| RNF-08 | Logging (Serilog), manejo de excepciones, validaciones con FluentValidation |
| RNF-09 | Cobertura de pruebas backend >= 90% |
| RNF-10 | Docker Compose para ejecución local |
| RNF-11 | Pipeline CI/CD con GitHub Actions |
| RNF-12 | Interfaz clara y coherente. Sin librerías UI externas (no Tailwind, no MUI) |
| RNF-13 | Autenticación delegada a Keycloak (OIDC + PKCE) |
| RNF-14 | Cada microservicio tiene su propia base de datos PostgreSQL |
| RNF-15 | API Gateway como único punto de entrada externo |

---

## Reglas de Negocio

| Código | Descripción |
|--------|-------------|
| RB-01 | Una misión solo puede usarse en sesiones si está en estado Activa. |
| RB-02 | Una sesión no puede iniciar sin al menos un equipo registrado. |
| RB-03 | No se aceptan respuestas ni evidencias si la sesión está Pausada, Finalizada o Cancelada. |
| RB-04 | Una pista no se libera dos veces al mismo equipo para la misma etapa. |
| RB-05 | Cada evidencia está asociada a un equipo, sesión y etapa específicos. |
| RB-06 | Toda penalización registra motivo y momento. |
| RB-07 | El puntaje acumulado tiene trazabilidad de origen (pista usada, tiempo, acierto). |
| RB-08 | El ranking se ordena por puntaje descendente; el tiempo actúa como desempate. |
| RB-09 | Los cambios de estado respetan las transiciones válidas (State Pattern). |
| RB-10 | Un operador administra únicamente las sesiones que él mismo creó (`Session.OperatorId`). El administrador consulta cualquier sesión en modo solo lectura, pero no la gestiona. |

---

## Modelo de Dominio Completo (para LucidChart)

Este modelo está dividido por bounded context (microservicio). Cada entidad incluye todos sus atributos con tipos para construir el diagrama de clases/ER.

### Bounded Context 1: Missions Service

```
┌─────────────────────────────────┐
│            Mission              │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + Title: string                 │
│ + Description: string           │
│ + Difficulty: MissionDifficulty │
│ + TimeMinutes: int              │
│ + Type: MissionType             │
│ + Status: MissionStatus         │
│ + CreatedAt: DateTime           │
└─────────────────┬───────────────┘
                  │ 1
                  │ has many
                  │ *
┌─────────────────▼───────────────┐
│          MissionStage           │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + MissionId: Guid (FK)          │
│ + Name: string                  │
│ + Description: string           │
│ + Order: int                    │
└─────────────────┬───────────────┘
                  │ 1
                  │ has many
                  │ *
┌─────────────────▼───────────────┐
│           MissionClue           │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + StageId: Guid (FK)            │
│ + Content: string               │
│ + PenaltyPoints: int            │
│ + ReleaseType: ClueReleaseType  │
└─────────────────────────────────┘

Enums:
  MissionDifficulty { Easy, Medium, Hard }
  MissionType       { Treasure, Trivia }
  MissionStatus     { Draft, Active, Inactive }
  ClueReleaseType   { Automatic, Manual }
```

### Bounded Context 2: Sessions Service

```
┌─────────────────────────────────┐
│            Session              │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + Name: string                  │
│ + Pin: string (6 dígitos)       │
│ + Status: SessionStatus         │
│ + CurrentStageOrder: int        │
│ + StartedAt: DateTime?          │
│ + EndedAt: DateTime?            │
│ + CreatedAt: DateTime           │
│ + Stages: List<SessionStage>    │  ← JSONB en PostgreSQL
│ + Participants: List<Participant>│  ← tabla separada
└───────────────┬─────────────────┘
                │ 1
                │ has many
                │ *
┌───────────────▼─────────────────┐
│       SessionParticipant        │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + SessionId: Guid (FK)          │
│ + UserId: Guid (ref Keycloak)   │
│ + UserAlias: string             │
│ + JoinedAt: DateTime            │
└─────────────────────────────────┘
                │ 1
                │ has many
                │ *
┌───────────────▼─────────────────┐
│           SessionTeam           │  ← reemplaza al "Teams Service" original
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + SessionId: Guid (FK)          │
│ + Name: string                  │
│ + MaxMembers: int (fijo en 5)   │
│ + Score: int                    │
│ + LastScoreAt: DateTime?        │  ← RB-08: desempate de ranking
│ + CreatedAt: DateTime           │
└───────────────┬─────────────────┘
                │ 1
                │ has many
                │ *
┌───────────────▼─────────────────┐
│        SessionTeamMember        │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + SessionTeamId: Guid (FK)      │
│ + UserId: Guid (ref Keycloak)   │
│ + UserAlias: string             │
│ + JoinedAt: DateTime            │
└─────────────────────────────────┘

Nota: no existe VO JoinCode. Un equipo se crea dentro de una sesión
(POST /{sessionId}/teams, rol operator) y el participante se une
directamente (POST /{sessionId}/teams/{teamId}/join, autenticado).

Value Object (serializado como JSONB):
┌─────────────────────────────────┐
│         SessionStage            │
├─────────────────────────────────┤
│ + MissionId: Guid               │
│ + MissionTitle: string          │
│ + MissionType: string           │
│ + Order: int                    │
└─────────────────────────────────┘

Enums:
  SessionStatus { Scheduled, Preparing, Active, Paused, Finished, Cancelled }

Máquina de Estados:
  Scheduled ──────→ Preparing ──────→ Active ⇄ Paused
                                           ↓         ↓
               Cancelled ←── (cualquier estado no terminal)
                                                 Finished

Endpoints adicionales (multi-mission):
  POST /{sessionId}/stages/{stageId}/advance  ← operador avanza de etapa
  POST /{sessionId}/stages/{stageId}/clues    ← operador libera pista
  GET  /{sessionId}/progress                  ← dashboard del operador
```

### Bounded Context 3: Trivia Service

```
┌─────────────────────────────────┐
│             Quiz                │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + Title: string                 │
└───────────────┬─────────────────┘
                │ 1
                │ has many
                │ *
┌───────────────▼─────────────────┐
│           Question              │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + QuizId: Guid (FK)             │
│ + Text: string                  │
│ + TimeLimitSeconds: int         │
│ + Order: int                    │
└───────────────┬─────────────────┘
                │ 1
                │ has many (2-4)
                │ *
┌───────────────▼─────────────────┐
│            Answer               │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + QuestionId: Guid (FK)         │
│ + Text: string                  │
│ + IsCorrect: bool               │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│        LeaderboardEntry         │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + QuizId: Guid (FK)             │
│ + TeamId: Guid (ref externo)    │
│ + TeamName: string              │
│ + Score: int                    │
│ + UpdatedAt: DateTime           │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│       ParticipantAnswer         │
├─────────────────────────────────┤
│ + Id: Guid (PK)                 │
│ + QuizId: Guid (FK)             │
│ + TeamId: Guid (ref externo)    │
│ + QuestionId: Guid (FK)         │
│ + AnswerId: Guid (FK)           │
│ + Timestamp: DateTime           │
│ + IsCorrect: bool               │
└─────────────────────────────────┘

Flujo de scoring (implementado):
  SubmitAnswer → TimeBasedScoringStrategy → ParticipantAnswer
              → TriviaAnswerSubmittedEvent (RabbitMQ)
              → TriviaAnswerSubmittedConsumer
              → UpdateLeaderboardCommand → LeaderboardEntry
              → POST /internal/events/LeaderboardUpdated
              → SignalR RankingUpdated
```

### Bounded Context 4: RealTimeHub (Sin persistencia)

```
┌─────────────────────────────────────────────────────────┐
│                      GameHub                             │
│                (SignalR Hub — /hub/game)                 │
├─────────────────────────────────────────────────────────┤
│ Métodos del Hub (cliente → servidor):                   │
│   JoinSessionGroup(sessionId)                           │
│   LeaveSessionGroup(sessionId)                          │
│                                                         │
│ Eventos enviados al cliente (servidor → cliente):       │
│                                                         │
│   SessionStatusChanged                                  │
│     payload: { sessionId, newStatus }                   │
│     trigger: Sessions.Service PATCH /status             │
│                                                         │
│   ProgressUpdated                                       │
│     payload: { sessionId, stages, participants }        │
│     trigger: Sessions.Service POST /advance             │
│                                                         │
│   ClueReleased                                          │
│     payload: { sessionId, stageId, clueContent }        │
│     trigger: Sessions.Service POST /clues               │
│                                                         │
│   QuestionAsked                                         │
│     payload: { questionId, text, answers, timeLimit }   │
│     trigger: Trivia.Service POST /questions/ask         │
│                                                         │
│   RankingUpdated                                        │
│     payload: List<LeaderboardEntry>                     │
│     trigger: TriviaAnswerSubmittedConsumer (RabbitMQ)   │
│                                                         │
│ Endpoints HTTP internos (usados por otros servicios):   │
│   POST /internal/notifications/session-status           │
│   POST /internal/notifications/progress                 │
│   POST /internal/notifications/clue-released            │
│   POST /internal/notifications/question-asked           │
│   POST /internal/events/LeaderboardUpdated              │
└─────────────────────────────────────────────────────────┘
```

### Relaciones entre Bounded Contexts (Referencias Externas)

```
Trivia.QuizSession.SessionId ─── ref ──→ Sessions.Session.Id
Trivia.ParticipantAnswer.TeamId ─── ref ──→ Sessions.SessionTeam.Id
Sessions.SessionTeamMember.UserId ─── ref ──→ Keycloak User.Id
Sessions.SessionParticipant.UserId ─── ref ──→ Keycloak User.Id
Missions.Operator/Participant records ─── ref ──→ Keycloak User.Id
```

> `SessionTeam` ya no es una referencia externa: vive dentro del propio bounded context de Sessions.Service (ver diagrama arriba).

**Nota LucidChart:** Las referencias entre servicios son foreign keys lógicas (sin constraint de BD). En el diagrama, se representan con líneas punteadas entre los bounded contexts.

---

## Patrones de Diseño Implementados

| Patrón | Dónde se aplica |
|--------|----------------|
| **State** | Ciclo de vida de sesiones — `SessionState`, `ScheduledState`, `InPreparationState`, etc. |
| **CQRS** | MediatR en cada microservicio — `CreateMissionCommand`, `GetSessionQuery`, etc. |
| **Repository** | Interfaces en Domain, implementaciones en Infrastructure para cada entidad |
| **Strategy** | Cálculo de puntaje por tipo de misión (`TreasureScoreStrategy`, `TriviaScoreStrategy`) |
| **Facade** | `GameSessionFacade` coordina múltiples servicios durante la sesión en vivo |
| **Proxy** | `MissionAccessProxy` — control de acceso por rol antes de delegar al servicio real |
| **Chain of Responsibility** | Validaciones de cambio de estado de sesión y de misión (`IMissionStatusHandler`) |
| **Composite** | `IMissionComponent` — `Mission`/`MissionStage`/`MissionClue` comparten `Validate()`, `GetTotalPenalty()`, `GetLeafCount()` |
| **Saga (Coreográfica)** | Coordinación de transacciones entre servicios vía RabbitMQ |
| **Outbox Pattern** | Garantiza consistencia eventual para eventos publicados en RabbitMQ |
| **API Gateway** | YARP — punto único de entrada con autenticación centralizada |
| **Database per Service** | Cada microservicio tiene su propia base PostgreSQL |

---

## Servicios — Detalles de Implementación

### ApiGateway (`:5000`)
- **Rol:** Reverse proxy YARP + validación JWT
- **Archivo clave:** `src/ApiGateway/Program.cs`, `KeycloakRolesTransformer.cs`
- **Políticas por ruta:**

| Ruta | Política | Destino |
|------|---------|---------|
| `POST /api/register` | anonymous | Missions.Service |
| `GET/PUT /api/profile` | `authenticated` | Missions.Service |
| `/hub/game/**` | anonymous (WebSocket) | RealTimeHub |
| `/health` | anonymous | — |
| `/api/missions/**` | `operator_or_admin` | Missions.Service |
| `/api/sessions/**` (incluye `/api/sessions/{id}/teams/**`) | `authenticated` | Sessions.Service |
| `/api/trivia/**` | `authenticated` | Trivia.Service |
| `/api/quizzes/**` | `operator_or_admin` | Trivia.Service |
| `/api/admin/operators/**` | `admin` | Missions.Service |
| `/api/admin/users/**` | `operator_or_admin` | Missions.Service |
| `/api/admin/missions/**` | `admin` | Missions.Service |

> No existe ruta `/api/teams/**` — la gestión de equipos vive bajo `/api/sessions/{sessionId}/teams/**`, ya enrutada por la regla `sessions`.

### Missions.Service (`:5001`)
- **DB:** `missions-db` (usuario: `missions`, pass: `missions123`)
- **Responsabilidad ampliada:** además del catálogo de misiones/etapas/pistas, aloja todo lo que habla con la **Keycloak Admin API** — registro de participantes, perfil, alta/baja de operadores, listado de usuarios (`POST /api/register`, `GET/PUT /api/profile`, `/api/admin/operators/**`, `/api/admin/users/**`)
- **Solución:** `src/Missions.Service/Missions.Service.slnx`

### Sessions.Service (`:5002`)
- **DB:** `sessions-db` (usuario: `sessions`, pass: `sessions123`)
- **Responsabilidad ampliada:** además del ciclo de vida de sesiones, aloja los equipos como sub-agregado (`SessionTeam`/`SessionTeamMember`) — `POST /{sessionId}/teams`, `GET /{sessionId}/teams`, `POST /{sessionId}/teams/{teamId}/join`, `DELETE /{sessionId}/teams/{teamId}/members/{userId}`
- **Variables Docker:** `RabbitMq__Host=rabbitmq`, `Missions__Url=http://missions.service:80`
- **Eventos publicados:** `session.status.changed` → `trivia.exchange` (topic)
- **Solución:** `src/Sessions.Service/Sessions.Service.slnx`

### Trivia.Service (`:5004`)
- **DB:** `trivia-db` (usuario: `trivia`, pass: `trivia123`, **puerto host: 5432**)
- **Consume de:** cola `trivia.answer.submitted` (exchange `trivia`, routing key `answer.submitted`) — `TriviaAnswerSubmittedConsumer` (BackgroundService, polling cada 500ms)
- **Endpoints activos:** `POST /quizzes`, `GET /quizzes/{id}`, `POST /start-trivia`, `POST /questions/ask`, `POST /answers`, `POST /games/{sessionId}/end`, `GET /ranking`
- **Scoring:** `TimeBasedScoringStrategy` — puntaje mayor si responde más rápido
- **Solución:** `src/Trivia.Service/Trivia.Service.slnx`

### RealTimeHub (`:5005`)
- **Hub:** `/hub/game` — `GameHub`
- **Sin base de datos** (estado en memoria)
- **CORS:** permisivo (solo para dev)

---

## Infraestructura

### Bases de Datos
| Base | Contenedor | Usuario | Password | Puerto Host |
|------|-----------|---------|----------|-------------|
| `missions` | `missions-db` | `missions` | `missions123` | — |
| `sessions` | `sessions-db` | `sessions` | `sessions123` | — |
| `trivia` | `trivia-db` | `trivia` | `trivia123` | `5432` |
| `keycloak` | `keycloak-db` | `keycloak` | `keycloak123` | — |

**Importante:** Todos usan `EnsureCreated()` — no hay migraciones EF Core. Si cambia el modelo, hay que recrear el contenedor de DB.

### Keycloak
- URL: `http://localhost:8080`
- Admin: `admin` / `admin123`
- Realm: `umbral` (importado desde `keycloak/realm-export.json`)
- Clientes: `umbral-frontend` (público, SPA), `umbral-gateway` (confidencial)
- Roles del realm: `admin`, `operator`, `participant`
- Atributos de usuario: `name` (string), `alias` (string, solo participantes)

### RabbitMQ
- AMQP: `:5672` | Management: `http://localhost:15672` (guest/guest)
- Exchange: `trivia.exchange` (type: topic)
- Cola activa: `trivia.answer.submitted`

### Puertos Locales
| Servicio | Puerto |
|----------|--------|
| API Gateway | `5000` |
| Missions | `5001` |
| Sessions | `5002` |
| Trivia | `5004` |
| RealTimeHub | `5005` |
| Frontend | `5173` |
| Keycloak | `8080` |
| RabbitMQ Management | `15672` |

---

## Frontend (React + Vite)

- **Entry:** `frontend/src/main.tsx`
- **Auth:** `oidc-client-ts` + `react-oidc-context` — sin formulario de login propio, redirige a Keycloak
- **Router:** `react-router-dom` v7
- **Tiempo real:** `@microsoft/signalr`
- **Testing:** Vitest + Testing Library + jsdom
- **Sin librerías de componentes** (sin Tailwind, sin MUI, sin Chakra — CSS plano)

### Estructura de páginas
```
pages/
├── admin/           # Vistas del administrador
│   ├── CatalogoMisiones.tsx, CrearMision.tsx, EditarMision.tsx, DetalleMision.tsx
│   ├── CrearOperador.tsx, DesactivarOperador.tsx
│   ├── ListadoUsuarios.tsx, DetalleUsuario.tsx
│   ├── CrearEquipo.tsx, EditarEquipo.tsx, EquipoDetalle.tsx, ListadoEquipos.tsx
│   ├── CrearSesion.tsx, ListadoSesiones.tsx, PanelSesion.tsx
│   └── QuizBank.tsx
├── operator/        # Vistas del operador
│   ├── IniciarJuego.tsx
│   └── QuestionResults.tsx
├── participant/     # Vistas del participante
│   ├── UnirseEquipo.tsx, UnirseSesion.tsx, MiPerfil.tsx
│   ├── NextStageRedirect.tsx
│   └── game/        # Vistas en vivo para el teléfono
└── public/          # Landing, login, registro
```

### Proxy de desarrollo (`vite.config.ts`)
- `/api` → `http://localhost:5000`
- `/hub` (WebSocket) → `http://localhost:5000`
- Variable de entorno: `VITE_API_URL`

---

## Cómo Trabajar en el Proyecto

### Arranque completo
```bash
docker compose up -d      # Esperar ~20s por Keycloak
cd frontend && npm ci && npm run dev
# Abrir http://localhost:5173
```

### Solo infraestructura (desarrollo de un servicio desde IDE)
```bash
docker compose up -d missions-db sessions-db trivia-db keycloak rabbitmq
cd src/Sessions.Service && dotnet run --project Sessions.Api
```

### Tests
```bash
dotnet test src/Missions.Service/Missions.Service.slnx
dotnet test src/Sessions.Service/Sessions.Service.slnx
dotnet test src/Trivia.Service/Trivia.Service.slnx
dotnet test src/ApiGateway.Tests/ApiGateway.Tests.csproj
dotnet test src/RealTimeHub.Tests/RealTimeHub.Tests.csproj
cd frontend && npm test
```

### Credenciales de prueba
| Rol | Usuario | Contraseña |
|-----|---------|------------|
| Admin | admin | admin123 |
| Operador | operator | operator123 |
| Participante | participante | participante123 |

---

## Convenciones de Código

- **Idioma del código:** INGLÉS (nombres, comentarios, commits, strings UI)
- **Documentación y comunicación:** Español
- **PascalCase** para clases/métodos, **camelCase** para variables/parámetros
- Un archivo por clase, nombrado como la clase
- **Conventional Commits:** `feat(HU-NN):`, `fix:`, `test:`, `chore:`, `docs:`
- **Tests:** xUnit + FluentAssertions + NSubstitute, nombres `Given_When_Then`
- **CQRS estricto:** Comandos para escribir, Queries para leer — no mezclar

---

## Git Workflow

```
main         ── Producción. Solo recibe merges de release/ y hotfix/
└── develop  ── Integración. Todas las features mergean aquí
    └── feature/HU-NN-descripcion  ── 1 HU = 1 rama = 1 commit = 1 PR a develop
```

**Regla:** `1 HU = 1 rama feature = 1 commit = 1 PR → develop`

---

## Problemas Conocidos

- **Keycloak tarda 15-20s en arrancar** — errores 401 en los primeros segundos son normales
- **PKCE requiere HTTPS** — `disablePKCE: true` en dev con HTTP
- **EF Core `EnsureCreated()` no migra esquemas** — cambios en el modelo requieren recrear el contenedor
- **Solo `trivia-db` expone puerto al host** — para conectar a otras DBs desde IDE, exponer el puerto en docker-compose
- **Tests deben ejecutarse por solución** — no hay solución raíz unificada
- **RealTimeHub CORS permisivo** — solo para dev, no para producción
- **Frontend IP dinámica** — `window.location.hostname` en vez de `localhost`. Hay que agregar la IP en Redirect URIs de Keycloak al cambiar de red

---

## Próximos Pasos Recomendados

1. **Implementar trivias en vivo (HU-36 a HU-45)** — mayor funcionalidad pendiente
2. **Integrar SignalR en el frontend** — cablear `GameHub` a vistas de operador y participante
3. **Mergear ramas `hu-28` a `hu-35`** del remote a develop
4. **Documentación OpenAPI/Swagger** accesible desde el Gateway

---

*CLAUDE.md generado en Julio 2026. Mantener actualizado con cada cambio arquitectónico significativo.*

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
