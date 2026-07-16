# UMBRAL E2E Tests (Playwright)

Tests end-to-end que validan flujos críticos contra el entorno completo
con Docker Compose.

## Requisitos

- Node.js 22+
- Docker + Docker Compose
- Navegador Chromium (se instala automáticamente con `npm run install`)

## Preparación

```bash
# 1. Levantar toda la infraestructura
docker compose up -d
# Esperar ~20s a que Keycloack termine de arrancar

# 2. Verificar que todo esté funcionando
curl -s http://localhost:5000/health
curl -s http://localhost:5173

# 3. Instalar dependencias de Playwright
cd tests/e2e
npm ci
npm run install   # descarga Chromium
```

## Ejecutar tests

```bash
# Desde tests/e2e/
npx playwright test          # headless
npx playwright test --headed # con navegador visible
npx playwright test --debug  # modo debug
```

## Estructura

```
tests/e2e/
├── package.json            # Dependencias de Playwright
├── playwright.config.ts    # Configuración (baseURL, reporters, etc.)
├── helpers.ts              # Funciones de ayuda (login, registro, etc.)
├── admin-flow.spec.ts      # Flujo admin: crear misión, crear sesión
├── README.md               # Este archivo
└── playwright-report/      # Reporte HTML generado tras la ejecución
```

## Usuarios de prueba

| Rol          | Usuario      | Contraseña     |
|--------------|--------------|----------------|
| Admin        | admin        | admin123       |
| Operador     | operator     | operator123    |
| Participante | participante | participante123|

## Notas

- Keycloak tarda ~15-20s en arrancar la primera vez.
- Los tests asumen que el frontend corre en `http://localhost:5173` y
  Keycloak en `http://localhost:8080`.
- Los tests se ejecutan en serie (workers=1) para no interferir entre sí.
- No se incluyen en el pipeline CI principal porque requieren toda la
  infraestructura levantada (Docker Compose).
