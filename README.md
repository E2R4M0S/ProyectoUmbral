# UMBRAL — Plataforma de Operación de Experiencias Inmersivas

Plataforma web para la operación en tiempo real de experiencias de investigación inmersiva tipo *escape room* académico. Desarrollado como proyecto integrador para la **UCAB** (Universidad Católica Andrés Bello), materia Desarrollo de Software.

**Autores:** Eros Dos Ramos (30.371.156) · Adrián Cereijo (29.756.926)

---

## Descripción

UMBRAL permite a un operador crear sesiones de juego con misiones de tipo **Búsqueda del Tesoro** (etapas, pistas) o **Trivia** (preguntas de opción múltiple con temporizador y puntuación automática). Equipos de participantes se unen vía PIN desde sus teléfonos. Todo el progreso del juego se transmite en tiempo real vía WebSockets.

### Roles

| Rol | Acceso | Responsabilidades |
|-----|--------|-------------------|
| **Administrador** | Desktop | Crea operadores, misiones, trivias y supervisa la plataforma |
| **Operador** | Desktop | Crea sesiones, gestiona equipos, libera pistas, opera partidas en vivo |
| **Participante** | Móvil | Se registra, se une a sesiones, responde trivias, consulta resultados |

---

## Arquitectura

> **Nota:** el `Teams.Service` planeado originalmente nunca quedó como microservicio independiente — fue absorbido: los equipos viven dentro de `Sessions.Service` (`SessionTeam`, sin JoinCode) y el registro/perfil/operadores/usuarios (Keycloak Admin API) quedaron en `Missions.Service`. Son **3 microservicios de dominio**, no 4.

```
                         Internet
                             │
            ┌────────────────▼────────────────┐
            │         API Gateway :5000        │
            │     YARP + Validación JWT        │
            │         (Keycloak JWKS)          │
            └──┬──────────────┬─────────────┬──┘
               │              │             │
   ┌───────────▼──────┐  ┌────▼────────┐  ┌─▼─────────────┐
   │  Missions :5001   │  │Sessions:5002│  │  Trivia :5004 │
   │  missions-db      │  │sessions-db  │  │  trivia-db    │
   │  ├ misiones/pistas│  │ ├ ciclo vida│  │  ├ quizzes    │
   │  ├ operadores      │  │ ├ SessionTeam│ │  └ ranking    │
   │  └ Keycloak Admin  │  │ └ auditoría │  └───────┬───────┘
   └────────────────────┘  └──┬──────────┘          │
                              │                      │
              ┌───────────────▼──────────────────────▼──────┐
              │              RabbitMQ :5672                  │
              │   exchange: trivia.exchange (topic)          │
              │   cola: trivia.answer.submitted              │
              └─────────────────────┬────────────────────────┘
                                    │
              ┌─────────────────────▼───────────────────────────────┐
              │         RealTimeHub :5005  (SignalR /hub/game)       │
              │  Eventos: SessionStatusChanged · ProgressUpdated     │
              │           ClueReleased · QuestionAsked · RankingUpdated│
              └─────────────────────────────────────────────────────┘
                                    │
              ┌─────────────────────▼───────────────────────────────┐
              │               Frontend React :5173                   │
              │   oidc-client-ts · @microsoft/signalr               │
              └─────────────────────────────────────────────────────┘
                                    │
              ┌─────────────────────▼───────────────────────────────┐
              │              Keycloak :8080                          │
              │   Realm: umbral · Roles: admin, operator, participant│
              └─────────────────────────────────────────────────────┘
```

### Arquitectura por microservicio: Hexagonal / Clean Architecture

```
Domain          → Entidades, Value Objects, interfaces de repositorio
Application     → Commands, Queries, Handlers, DTOs (MediatR + CQRS)
Infrastructure  → EF Core, Repositorios, RabbitMQ, Keycloak Admin API
Api             → Minimal API endpoints, Program.cs
```

---

## Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 10 (C#), Minimal APIs |
| Frontend | React 19 + TypeScript 6 + Vite 8 |
| Base de datos | PostgreSQL 16 (una por microservicio) |
| ORM | Entity Framework Core 10 (`EnsureCreated()`) |
| CQRS | MediatR + FluentValidation |
| Autenticación | Keycloak 26.1 (OIDC + Authorization Code) |
| Tiempo real | SignalR (WebSockets) |
| Mensajería | RabbitMQ 4 (topic exchange) |
| API Gateway | YARP (Yet Another Reverse Proxy) |
| Testing | xUnit + FluentAssertions + NSubstitute (backend), Vitest (frontend) |
| Contenedores | Docker + Docker Compose |

---

## Inicio Rápido

### Prerequisitos

- Docker Desktop
- Node.js 20+
- (Opcional para desarrollo) .NET 10 SDK

### Arrancar todo con Docker

```bash
# Clonar el repositorio
git clone https://github.com/E2R4M0S/ProyectoUmbral.git
cd ProyectoUmbral

# Levantar infraestructura + servicios
docker compose up -d

# El API Gateway tarda ~5s. Keycloak tarda ~20s en estar listo.
# Verificar que todo está corriendo:
docker compose ps
```

### Arrancar el frontend

```bash
cd frontend
npm install
npm run dev
```

Abrir [http://localhost:5173](http://localhost:5173)

### Credenciales de prueba

| Rol | Usuario | Contraseña |
|-----|---------|------------|
| Administrador | `admin` | `admin123` |
| Operador | `operator` | `operator123` |
| Participante | `participante` | `participante123` |

---

## Servicios y Puertos

| Servicio | URL local | Descripción |
|----------|-----------|-------------|
| Frontend | http://localhost:5173 | SPA React |
| API Gateway | http://localhost:5000 | Punto único de entrada |
| Missions Service | http://localhost:5001 | CRUD de misiones + usuarios/operadores (Keycloak Admin API) |
| Sessions Service | http://localhost:5002 | Ciclo de vida de sesiones + equipos (`SessionTeam`) |
| Trivia Service | http://localhost:5004 | Quizzes y juego en vivo |
| RealTimeHub | http://localhost:5005 | SignalR WebSocket hub |
| Keycloak | http://localhost:8080 | Identity Provider |
| RabbitMQ UI | http://localhost:15672 | Management (guest/guest) |

---

## Flujo de Juego

### Modo Búsqueda del Tesoro

```
Admin  → Crear misión (tipo Tesoro, con etapas y pistas)
Admin  → Activar misión

Operador → Crear sesión (seleccionar misión/s) → obtiene PIN
Operador → Preparar sesión

Participante → Registrarse → Unirse a sesión (con PIN) → WaitingRoom

Operador → Iniciar partida → todos pasan a ActiveGame
         → Panel en tiempo real: ve progreso de cada etapa
         → Avanzar etapa (operador controla el ritmo)
         → Enviar pista (penaliza puntos, aparece en teléfono del equipo)

Operador → Finalizar sesión → resultados vía SignalR
```

### Modo Trivia

```
Admin  → Crear quiz → Agregar preguntas → Definir respuestas → Marcar correcta
       → Crear misión (tipo Trivia, vinculada al quiz)

Operador → Crear sesión → Preparar → Iniciar trivia
         → Lanzar pregunta → temporizador corre en todos los teléfonos
         → Participantes responden (bloqueo inmediato tras selección)
         → Cerrar pregunta → ranking actualizado vía RabbitMQ + SignalR
         → Siguiente pregunta → ...
         → Finalizar juego → podio final

Participante → Ve preguntas en su teléfono → responde → ve si acertó → ve ranking
```

---

## Funcionalidades Implementadas

### Módulo 1: Accesos y Usuarios (HU-01 a HU-06) — Completo
- Login/logout vía Keycloak OIDC (sin formulario propio)
- Registro de participantes (Keycloak Admin API)
- Creación y desactivación de operadores (con revocación de tokens)
- Modificación de perfil sincronizada con Keycloak
- Listado de usuarios con filtros por rol y estado

### Módulo 2: Misiones (HU-07 a HU-14) — Completo
- CRUD de misiones (tipo Tesoro o Trivia)
- Gestión de etapas y pistas por misión
- Activación/desactivación con validaciones de negocio

### Módulo 3: Equipos (HU-15 a HU-19) — Completo
- Equipos como sub-agregado de `Sessions.Service` (`SessionTeam`), creados dentro de una sesión ya existente — **sin JoinCode** (el requerimiento original de un VO `JoinCode` fue reemplazado por esta decisión de diseño)
- Gestión de miembros (el operador quita miembros; el participante se une directamente estando autenticado, máx. 5 por equipo)

### Módulo 4: Sesiones en Vivo (HU-20 a HU-27) — Completo
- Sesiones **multi-misión** (composición de varias misiones en secuencia)
- PIN de 6 dígitos para unirse
- State Machine completo: `Scheduled → Preparing → Active ⇄ Paused → Finished/Cancelled`
- Dashboard del operador con progreso en tiempo real
- Liberación manual de pistas
- Avance de etapa controlado por el operador

### Módulo 5: Gestión de Trivias (HU-28 a HU-35) — Implementado (pendiente merge)
- CRUD de cuestionarios, preguntas y respuestas
- Marcado de respuesta correcta
- Hasta 4 opciones por pregunta con tiempo límite configurable

### Módulo 6: Trivia en Vivo (HU-36 a HU-47) — Parcialmente implementado
- Comandos de backend listos: `StartTrivia`, `AskQuestion`, `SubmitAnswer`, `CloseQuestion`, `EndTriviaGame`
- Scoring con estrategia basada en tiempo (`TimeBasedScoringStrategy`)
- Flujo asíncrono RabbitMQ → Leaderboard → SignalR `RankingUpdated`
- Integración frontend pendiente

---

## Estructura del Repositorio

```
ProyectoUmbral/
├── src/
│   ├── ApiGateway/                    # YARP Reverse Proxy + JWT validation
│   │   ├── Program.cs
│   │   └── KeycloakRolesTransformer.cs
│   ├── ApiGateway.Tests/
│   │
│   ├── Missions.Service/              # Clean Architecture
│   │   ├── Missions.Api/              # Minimal API endpoints (misiones + register/profile/operators/users)
│   │   ├── Missions.Application/      # Commands, Queries, Handlers
│   │   ├── Missions.Domain/           # Mission, MissionStage, MissionClue, Participant
│   │   ├── Missions.Infrastructure/   # EF Core, Repositories, KeycloakAdminService
│   │   ├── Missions.Domain.Tests/
│   │   └── Missions.Service.slnx
│   │
│   ├── Sessions.Service/
│   │   ├── Sessions.Api/
│   │   ├── Sessions.Application/
│   │   ├── Sessions.Domain/           # Session, SessionStage (JSONB), SessionParticipant, SessionTeam(Member)
│   │   ├── Sessions.Infrastructure/   # EF Core, RabbitMQ publisher, GameSessionFacade
│   │   ├── Sessions.Domain.Tests/
│   │   └── Sessions.Service.slnx
│   │
│   ├── Trivia.Service/
│   │   ├── Trivia.Api/
│   │   ├── Trivia.Application/        # StartTrivia, AskQuestion, SubmitAnswer...
│   │   ├── Trivia.Domain/             # Quiz, Question, Answer, LeaderboardEntry
│   │   ├── Trivia.Infrastructure/     # EF Core, TriviaAnswerSubmittedConsumer
│   │   ├── Trivia.Domain.Tests/
│   │   └── Trivia.Service.slnx
│   │
│   └── RealTimeHub/                   # SignalR Hub (/hub/game)
│       ├── GameHub.cs
│       ├── NotificationEndpoints.cs   # HTTP endpoints internos
│       └── RealTimeHub.Tests/
│
├── frontend/
│   └── src/
│       ├── pages/
│       │   ├── admin/                 # CatalogoMisiones, CrearSesion, PanelSesion...
│       │   ├── operator/              # IniciarJuego, QuestionResults
│       │   ├── participant/game/      # WaitingRoom, ActiveGame, GameResults
│       │   └── public/               # Registro
│       ├── components/game/           # ClueCard, CountdownTimer, QuestionCard...
│       ├── services/                  # missionsApi, sessionsApi, teamsApi...
│       ├── hooks/useSignalR.ts
│       ├── contexts/GameContext.tsx
│       └── auth/                      # Keycloak + OIDC
│
├── keycloak/
│   └── realm-export.json              # Realm "umbral" pre-configurado
│
├── tests/e2e/                         # Playwright E2E (admin-flow.spec.ts)
├── .github/workflows/ci.yml           # Build+test por servicio, E2E en push a develop/main
├── docker-compose.yml
├── CLAUDE.md                          # Contexto completo para IA
└── README.md                          # Este archivo
```

---

## Desarrollo

### Solo infraestructura (para correr servicios desde el IDE)

```bash
docker compose up -d missions-db sessions-db trivia-db keycloak rabbitmq
```

### Correr un servicio individualmente

```bash
# Ejemplo: Sessions Service
cd src/Sessions.Service
dotnet run --project Sessions.Api

# Variables de entorno necesarias están en docker-compose.yml
# Si corrés desde el IDE, configurar:
# RabbitMq__Host=localhost
# ConnectionStrings__DefaultConnection=Host=localhost;...
# Missions__Url=http://localhost:5001
```

### Tests

```bash
# Backend — por solución de cada servicio
dotnet test src/Missions.Service/Missions.Service.slnx
dotnet test src/Sessions.Service/Sessions.Service.slnx
dotnet test src/Trivia.Service/Trivia.Service.slnx
dotnet test src/ApiGateway.Tests/ApiGateway.Tests.csproj
dotnet test src/RealTimeHub.Tests/RealTimeHub.Tests.csproj

# Frontend
cd frontend && npm test

# E2E (requiere docker compose up -d completo)
cd tests/e2e && npm ci && npx playwright test
```

CI (`.github/workflows/ci.yml`) corre build+test por servicio en cada push/PR a `develop`/`main`, más un job de E2E con Playwright solo en push (levanta el stack completo con Docker Compose). Objetivo de cobertura backend: RNF-09, >= 90%.

---

## Patrones de Diseño Implementados

| Patrón | Implementación |
|--------|---------------|
| **CQRS** | MediatR en todos los servicios — Commands y Queries separados |
| **State** | Ciclo de vida de `Session` — 6 estados con transiciones validadas |
| **Repository** | Interfaces en Domain, implementaciones en Infrastructure |
| **Strategy** | `TimeBasedScoringStrategy` para puntuación de trivia |
| **Facade** | `GameSessionFacade` coordina transiciones y notificaciones |
| **Proxy** | `MissionAccessProxy` — control de acceso por rol |
| **Chain of Responsibility** | Validaciones de cambio de estado (`BaseMissionStatusHandler`, `ValidStatusHandler`/`NotTerminalHandler`) |
| **Composite** | `IMissionComponent` — `Mission`/`MissionStage`/`MissionClue` comparten `Validate()`/`GetTotalPenalty()`/`GetLeafCount()` |
| **Saga** | Coordinación asíncrona entre servicios vía RabbitMQ |
| **Outbox** | Consistencia eventual para eventos publicados |

---

## API Reference

### Missions Service (vía Gateway → `:5001`)

Catálogo de misiones **y** todo lo que habla con la Keycloak Admin API (registro, perfil, operadores, usuarios) — ver nota de arquitectura al inicio de este README.

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| `GET` | `/api/missions` | Catálogo paginado | operator/admin |
| `GET` | `/api/missions/active` | Solo misiones activas | operator/admin |
| `GET` | `/api/missions/{id}` | Detalle completo | — |
| `POST` | `/api/missions` | Crear misión | admin |
| `PUT` | `/api/missions/{id}` | Editar misión | admin |
| `PATCH` | `/api/missions/{id}/status` | Activar/Desactivar | admin |
| `POST` | `/api/missions/{id}/stages` | Agregar etapa | admin |
| `POST` | `/api/missions/{id}/stages/{sid}/update` | Editar etapa | admin |
| `DELETE` | `/api/missions/{id}/stages/{sid}` | Eliminar etapa | admin |
| `POST` | `/api/missions/{id}/stages/{sid}/clues` | Agregar pista | admin |
| `DELETE` | `/api/missions/{id}/stages/{sid}/clues/{cid}` | Eliminar pista | admin |
| `POST` | `/api/register` | Registrar participante | anonymous |
| `GET` | `/api/profile` | Mi perfil | authenticated |
| `PUT` | `/api/profile` | Actualizar perfil | authenticated |
| `POST` | `/api/admin/operators` | Crear operador | admin |
| `PATCH` | `/api/admin/operators/{id}/disable` | Desactivar operador | admin |
| `GET` | `/api/admin/users` | Listado de usuarios | operator/admin |
| `GET` | `/api/admin/users/{id}` | Detalle de usuario | operator/admin |

### Sessions Service (vía Gateway → `:5002`)

Ciclo de vida de sesiones **y** equipos como sub-agregado de sesión (sin JoinCode).

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| `POST` | `/api/sessions` | Crear sesión (multi-misión) | operator/admin |
| `GET` | `/api/sessions` | Listado de sesiones | operator/admin |
| `GET` | `/api/sessions/{id}` | Detalle de sesión | operator/admin |
| `PATCH` | `/api/sessions/{id}/status` | Cambiar estado | operator/admin |
| `POST` | `/api/sessions/{id}/join` | Participante se une con PIN | authenticated |
| `GET` | `/api/sessions/{id}/progress` | Dashboard del operador | operator/admin |
| `POST` | `/api/sessions/{id}/stages/{sid}/advance` | Avanzar de etapa | operator/admin |
| `POST` | `/api/sessions/{id}/stages/{sid}/clues` | Liberar pista | operator/admin |
| `POST` | `/api/sessions/{id}/teams` | Crear equipo dentro de la sesión | operator |
| `GET` | `/api/sessions/{id}/teams` | Listar equipos de la sesión | authenticated |
| `POST` | `/api/sessions/{id}/teams/{tid}/join` | Unirse a un equipo (sin código) | authenticated |
| `DELETE` | `/api/sessions/{id}/teams/{tid}/members/{uid}` | Quitar miembro | operator |

### Trivia Service (vía Gateway → `:5004`)

| Método | Ruta | Descripción | Rol |
|--------|------|-------------|-----|
| `POST` | `/api/quizzes` | Crear quiz | admin |
| `GET` | `/api/quizzes` | Listar quizzes | admin |
| `GET` | `/api/quizzes/{id}` | Detalle del quiz | admin |
| `POST` | `/api/start-trivia` | Iniciar juego de trivia | operator/admin |
| `POST` | `/api/questions/ask` | Lanzar pregunta | operator/admin |
| `GET` | `/api/questions/{id}/answer-count` | Respuestas recibidas | operator/admin |
| `POST` | `/api/answers` | Enviar respuesta | authenticated |
| `POST` | `/api/games/{sessionId}/end` | Finalizar trivia | operator/admin |
| `GET` | `/api/ranking` | Leaderboard actual | authenticated |

### RealTimeHub — SignalR

**Conectar:** `new HubConnectionBuilder().withUrl("/hub/game").build()`

**Unirse a sesión:**
```javascript
await connection.invoke("JoinSessionGroup", sessionId);
```

**Escuchar eventos:**
```javascript
connection.on("SessionStatusChanged", ({ sessionId, newStatus }) => { ... });
connection.on("ProgressUpdated",       ({ sessionId, stages })   => { ... });
connection.on("ClueReleased",          ({ stageId, clueContent }) => { ... });
connection.on("QuestionAsked",         ({ questionId, text, answers, timeLimit }) => { ... });
connection.on("RankingUpdated",        (leaderboard) => { ... });
```

---

## Modelo de Dominio

```
Missions Service          Sessions Service
─────────────────         ────────────────
Mission                   Session
  ├── MissionStage    ←ref─  ├── SessionStage[]
  │   └── MissionClue        ├── SessionParticipant (userId + alias)
  ├── MissionType            └── SessionTeam
  │   Treasure / Trivia          └── SessionTeamMember (userId)
  └── MissionStatus
      Draft / Active /       SessionStatus:
      Inactive                 Scheduled → Preparing
                                → Active ⇄ Paused
Participant / Operator          → Finished / Cancelled
  (Keycloak Admin API)

Trivia Service                              RealTimeHub
──────────────                              ───────────
Quiz                                        GameHub (SignalR)
  └── Question                              ├── JoinSessionGroup()
      └── Answer (2-4)                      └── Eventos:
                                                SessionStatusChanged
LeaderboardEntry                                ProgressUpdated
ParticipantAnswer ──→ TimeBasedScoringStrategy  ClueReleased
                                                QuestionAsked
                                                RankingUpdated
```

---

## Git Workflow

```
main     ── producción, solo recibe releases y hotfixes
develop  ── integración, todas las features mergean aquí
  └── feature/HU-NN-descripcion  ── 1 HU = 1 rama = 1 commit = 1 PR
```

**Formato de commits:**
```
feat(HU-07): crear misión con tipo Tesoro/Trivia
fix(HU-22): corregir transición de Paused a Active
test(HU-07): agregar tests unitarios de CreateMission
chore: configurar Docker Compose con Keycloak
```

---

## Problemas Conocidos

| Problema | Causa | Solución |
|----------|-------|----------|
| Errores 401 al arrancar | Keycloak tarda ~20s en inicializar | Esperar y reintentar |
| PKCE no funciona | `crypto.subtle` requiere HTTPS | `disablePKCE: true` en dev, activar en producción con HTTPS |
| Cambios de modelo no persisten | `EnsureCreated()` no migra | Eliminar el contenedor de DB y recrearlo |
| No se puede conectar a las DBs desde el IDE | Solo `trivia-db` expone puerto al host | Agregar `ports` al servicio en docker-compose |
| Tests no corren desde la raíz | Sin solución unificada | Usar `dotnet test src/<Servicio>/<Servicio>.Service.slnx` |
| IP nueva no funciona en Keycloak | Redirect URIs fijos | Agregar la IP en Valid Redirect URIs del cliente `umbral-frontend` en Keycloak |

---

## Próximos Pasos

1. Completar integración frontend de trivia en vivo (HU-36 a HU-45)
2. Mergear ramas `hu-28` a `hu-35` del remote a `develop`
3. Cablear SignalR en vistas del participante (`WaitingRoom` → `ActiveGame`)
4. Documentación OpenAPI/Swagger accesible desde el Gateway

---

*UMBRAL v2 — Arquitectura de Microservicios + Keycloak — UCAB 2026*
