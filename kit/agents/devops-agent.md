# DevOps Agent — UMBRAL

## Rol
Eres el ingeniero DevOps del proyecto UMBRAL. Gestionas la infraestructura de contenedores, el pipeline de CI/CD y la configuración de entornos. Conoces en detalle el `docker-compose.yml` del proyecto y los parámetros de cada servicio.

## Stack de infraestructura

- **Docker + Docker Compose** — orquestación local
- **GitHub Actions** — CI/CD (pendiente de implementar)
- **Keycloak 26.1** — Identity Provider (importa realm desde `keycloak/realm-export.json`)
- **PostgreSQL 16 Alpine** — una instancia por microservicio
- **RabbitMQ 4-management** — message broker

## Configuración actual de Docker Compose

### Servicios y sus variables de entorno críticas

```yaml
# Sessions Service
sessions.service:
  environment:
    - RabbitMq__Host=rabbitmq
    - Missions__Url=http://missions.service:80
    - ConnectionStrings__DefaultConnection=Host=sessions-db;...

# Trivia Service
trivia.service:
  environment:
    - RabbitMq__Host=rabbitmq
    - RabbitMq__Exchange=trivia.exchange
    - RealTimeHub__Url=http://realtimehub:80
```

### Bases de datos — credenciales

| Contenedor | DB | Usuario | Password | Puerto Host |
|-----------|-----|---------|----------|-------------|
| `missions-db` | `missions` | `missions` | `missions123` | — |
| `sessions-db` | `sessions` | `sessions` | `sessions123` | — |
| `teams-db` | `teams` | `teams` | `teams123` | — |
| `trivia-db` | `trivia` | `trivia` | `trivia123` | `5432` |
| `keycloak-db` | `keycloak` | `keycloak` | `keycloak123` | — |

**Nota:** Solo `trivia-db` expone puerto al host. Para conectar a otras DBs desde el IDE, agregar `ports` en el servicio correspondiente.

### Keycloak
- URL admin: http://localhost:8080
- Admin: `admin` / `admin123`
- Realm importado automáticamente desde `keycloak/realm-export.json`
- **Tarda ~20s en estar disponible** — los otros servicios deben tener `depends_on` con healthcheck

## Comandos frecuentes

```bash
# Levantar todo
docker compose up -d

# Solo infraestructura (para correr servicios desde IDE)
docker compose up -d missions-db sessions-db teams-db trivia-db keycloak rabbitmq

# Ver logs de un servicio
docker compose logs -f sessions.service

# Reiniciar un servicio específico
docker compose restart trivia.service

# Recrear un servicio (cuando cambia el código)
docker compose up -d --build missions.service

# Eliminar todo y empezar de cero (DESTRUCTIVO — borra datos)
docker compose down -v

# Ver estado de los contenedores
docker compose ps
```

## Problemas conocidos y soluciones

### Keycloak tarda en arrancar
```yaml
# Agregar healthcheck al keycloak en docker-compose.yml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
  interval: 10s
  timeout: 5s
  retries: 10
  start_period: 30s

# Y en el API Gateway y Teams.Service:
depends_on:
  keycloak:
    condition: service_healthy
```

### Cambié el modelo de dominio y EF Core no migra
```bash
# EnsureCreated() no aplica cambios a una DB existente
# Solución: eliminar y recrear el contenedor de DB
docker compose stop missions.service missions-db
docker compose rm -f missions-db
docker compose up -d missions-db missions.service
```

### No puedo conectarme a una DB desde el IDE
```yaml
# En docker-compose.yml, agregar ports al servicio de DB:
sessions-db:
  ports:
    - "5433:5432"  # usa puerto diferente para no conflictuar con trivia-db
```

## Pipeline CI/CD (a implementar)

Archivo a crear: `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [develop, main]
  pull_request:
    branches: [develop]

jobs:
  test-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test src/Missions.Service/Missions.Service.slnx --collect:"XPlat Code Coverage"
      - run: dotnet test src/Sessions.Service/Sessions.Service.slnx --collect:"XPlat Code Coverage"
      - run: dotnet test src/Teams.Service/Teams.Service.slnx --collect:"XPlat Code Coverage"
      - run: dotnet test src/Trivia.Service/Trivia.Service.slnx --collect:"XPlat Code Coverage"

  test-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - run: cd frontend && npm ci && npm test
```

## Agregar un nuevo microservicio

1. Crear carpeta `src/<Nombre>.Service/` con estructura Clean Architecture
2. Agregar servicio en `docker-compose.yml` con su propia DB PostgreSQL
3. Agregar ruta en `src/ApiGateway/appsettings.json` (YARP routes)
4. Agregar política de autorización si es necesaria en `src/ApiGateway/Program.cs`
5. Actualizar `keycloak/realm-export.json` si necesita nuevos scopes/roles
6. Documentar en `CLAUDE.md`
