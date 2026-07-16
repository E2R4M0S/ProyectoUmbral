# Deploy público (para que el profesor pueda usar la app sin VPN)

Cubre el acceso **por navegador** (operador/admin y participante web) desde una PC
que corre todo el stack localmente, expuesta a internet vía **Cloudflare Tunnel**
(Quick Tunnel, sin cuenta ni dominio propio). El uso desde la APK contra este mismo
servidor es un paso aparte — ver el final de este archivo.

## Por qué así y no un VPS en la nube

Oracle Cloud (y varios proveedores más) dan problemas de acceso desde IPs de
Venezuela sin VPN, lo cual inutiliza esa opción si el profesor va a entrar sin VPN.
La alternativa: correr el stack en tu propia PC (con Docker, tal como ya lo tenés
hoy) y exponerlo con un túnel de Cloudflare. Cloudflare no geobloquea Venezuela, es
gratis, da HTTPS automático, y no requiere abrir puertos en tu router ni pelear con
firewalls de un cloud restringido.

**Trade-off:** tu PC tiene que estar prendida y con Docker corriendo mientras el
profesor evalúa. La URL pública (`https://palabras-random.trycloudflare.com`)
cambia cada vez que reiniciás el túnel — hay que generarla y avisarla justo antes
de la sesión de evaluación.

## 0. Requisitos

- Docker Desktop corriendo (ya lo tenés).
- `cloudflared` instalado. En Windows:
  ```powershell
  winget install --id Cloudflare.cloudflared
  ```
  (o descargá el binario desde https://github.com/cloudflare/cloudflared/releases)

## 1. Levantar el stack en modo "producción" (Caddy unificando todo en un origen)

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Esto agrega Caddy en el puerto 80 como único punto de entrada: la SPA y `/api`/`/hub`
van al frontend, y `/auth/*` va a Keycloak — todo bajo el mismo origen, porque el
Quick Tunnel solo expone **una** dirección local a la vez.

Esperá 1-2 minutos (Keycloak tarda en arrancar).

## 2. Levantar el túnel

```bash
cloudflared tunnel --url http://localhost:80
```

Va a imprimir algo como:

```
https://palabras-al-azar-1234.trycloudflare.com
```

Esa es la URL pública. Dejá esta terminal abierta mientras dure la evaluación —
cerrarla mata el túnel.

## 3. Registrar la URL del túnel en Keycloak (cada vez que reiniciás el túnel)

Keycloak solo soporta `*` como comodín al **final** de una redirect URI (ej.
`https://midominio.com/*`), no en medio del host — así que un patrón como
`https://*.trycloudflare.com/*` nunca hace match con la URL real que genera el
Quick Tunnel. Hay que cargar la URL exacta cada vez que `cloudflared` te da una
nueva. Hay un script que lo hace en un comando:

```bash
node scripts/register-tunnel-redirect.js https://palabras-al-azar-1234.trycloudflare.com
```

(usá la URL que te imprimió `cloudflared` en el paso anterior). Es aditivo e
idempotente — no rompe ni duplica nada si lo corrés de nuevo con la misma URL, y
usa las credenciales admin de Keycloak del `.env`/docker-compose (`admin`/`admin123`
por defecto en dev; sobreescribible con `KEYCLOAK_ADMIN_PASSWORD` si las cambiaste).

## 4. Verificar

- Abrí la URL de trycloudflare.com que imprimió `cloudflared` → debería cargar el
  login y redirigir a Keycloak bajo `/auth/...` con HTTPS válido (lo pone Cloudflare,
  no Caddy).
- Logs si algo falla: `docker compose logs caddy` / `docker compose logs keycloak`.

## Actualizar cuando cambies el código

```bash
git pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

El túnel (`cloudflared`) no necesita reiniciarse por esto — solo si vos lo cerrás,
en cuyo caso al volver a levantarlo te va a dar una URL nueva (avisale al profesor).

## APK sin estar en la misma red

La pantalla "Configurar servidor" de la app (o el link "cambiar" en la pantalla
de inicio si ya tenía un host guardado) acepta dos formatos:

- **IP de LAN** (ej. `192.168.0.3`): pega directo a los puertos de cada servicio
  (5000, 8080, 5005) — solo funciona si el teléfono está en la misma red que la PC.
- **URL completa del túnel** (ej. `https://palabras-al-azar-1234.trycloudflare.com`,
  la que imprimió `cloudflared` en el paso 2): la app pasa a rutear todo por ese
  único origen (`/api`, `/hub/game`, `/auth`) igual que el navegador — no necesita
  estar en la misma red.

No hace falta ningún build distinto de la APK para esto — el mismo `.apk` sirve
para ambos modos, solo cambia lo que se escribe en "Configurar servidor".
