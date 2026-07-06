---
description: Reglas del proyecto UMBRAL — microservicios, Keycloak, arquitectura hexagonal. Siempre activas.
alwaysApply: true
---

# Project Rules — UMBRAL v2

## Identidad del proyecto

**UMBRAL** es una plataforma de escape room académico en tiempo real.
- Universidad: UCAB — Desarrollo de Software
- Autores: Eros Dos Ramos y Adrián Cereijo
- Stack: ASP.NET Core 10 + React 19 + PostgreSQL × 5 + Keycloak + RabbitMQ + SignalR + YARP

## Reglas que nunca se rompen

1. **Keycloak es el único proveedor de identidad.** No implementes JWT propio, no agregues `PasswordHasher`, no agregues `TokenService`. Todo pasa por Keycloak.
2. **Database per service.** Cada microservicio tiene su propia PostgreSQL. Nunca compartas un DbContext entre servicios.
3. **Sin Tailwind, sin MUI, sin Chakra, sin Bootstrap.** CSS plano o módulos CSS en el frontend.
4. **El código va en inglés.** Variables, clases, métodos, commits, strings en la UI → INGLÉS. Documentación y comunicación con el usuario → Español.
5. **Arquitectura hexagonal estricta.** `Domain` no conoce `Infrastructure`. `Application` no conoce `Infrastructure`. Las interfaces van en `Domain`, las implementaciones en `Infrastructure`.
6. **CQRS estricto.** Commands para escribir, Queries para leer. No mezcles. Un `Command` no puede tener un return con datos de lectura significativos (solo ID o confirmación).

## Servicios y sus puertos

| Servicio | Puerto | Solución |
|----------|--------|----------|
| API Gateway | 5000 | `src/ApiGateway/` |
| Missions.Service | 5001 | `src/Missions.Service/Missions.Service.slnx` |
| Sessions.Service | 5002 | `src/Sessions.Service/Sessions.Service.slnx` |
| Teams.Service | 5003 | `src/Teams.Service/Teams.Service.slnx` |
| Trivia.Service | 5004 | `src/Trivia.Service/Trivia.Service.slnx` |
| RealTimeHub | 5005 | `src/RealTimeHub/` |
| Frontend | 5173 | `frontend/` |
| Keycloak | 8080 | Docker |

## Estructura de cada microservicio

```
<Servicio>.Domain/          → Entities, Value Objects, IRepository interfaces, Enums
<Servicio>.Application/     → Commands/, Queries/, Handlers/, DTOs/, Validators/
<Servicio>.Infrastructure/  → DbContext, Repositories, RabbitMQ, Keycloak, Services
<Servicio>.Api/             → Endpoints (Minimal API), Program.cs, DependencyInjection
<Servicio>.*.Tests/         → Tests xUnit
```

## Flujo al implementar una HU nueva

1. Crear rama `feature/HU-NN-descripcion` desde `develop`
2. Implementar en este orden: Domain → Application (Command/Query + Handler + Validator) → Infrastructure (Repository) → Api (endpoint) → Tests
3. 1 commit por HU completa: `feat(HU-NN): descripción en inglés`
4. PR a `develop`

## Comunicación entre servicios

- **Preferir asíncrono (RabbitMQ):** exchange `trivia.exchange` (topic), routing keys con formato `domain.entity.action` (ej: `session.status.changed`)
- **Solo síncrono cuando es necesario:** HTTP directo entre servicios internos (no pasan por el Gateway)
- **El Gateway no se usa para comunicación entre servicios**, solo para tráfico externo

## Qué hacer cuando hay ambigüedad

- Consultar `CLAUDE.md` en la raíz del proyecto — tiene las 47 HUs completas con criterios de aceptación
- Consultar `.cursor/specs/` para especificaciones técnicas detalladas
- Si un patrón existe en el proyecto (ej: cómo se implementa un Command), SIEMPRE seguir ese patrón existente
