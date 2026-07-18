#!/usr/bin/env node
// Registra la URL pública de un Cloudflare Quick Tunnel (https://algo.trycloudflare.com)
// como redirect URI / web origin válido del cliente umbral-frontend en Keycloak, y
// actualiza KC_HOSTNAME_URL para que los enlaces en correos usen la URL del túnel.
//
// Necesario porque Keycloak solo soporta wildcard como sufijo al final de una URI
// (ej. "https://midominio.com/*"), NO en medio del host — así que un patrón como
// "https://*.trycloudflare.com/*" nunca hace match con la URL real que genera el
// Quick Tunnel en cada arranque, y hay que cargar la URL exacta cada vez.
//
// Uso:
//   node scripts/register-tunnel-redirect.js https://palabras-al-azar-1234.trycloudflare.com
//
// Variables de entorno opcionales (default: valores de dev del docker-compose):
//   KEYCLOAK_BASE_URL (default http://localhost:8080/auth)
//   KEYCLOAK_ADMIN_USER (default admin)
//   KEYCLOAK_ADMIN_PASSWORD (default admin123)
//   KEYCLOAK_REALM (default umbral)
//   KEYCLOAK_CLIENT_ID (default umbral-frontend)

const tunnelUrl = process.argv[2];
if (!tunnelUrl || !/^https:\/\/.+\.trycloudflare\.com$/.test(tunnelUrl)) {
  console.error("Uso: node scripts/register-tunnel-redirect.js https://xxxx.trycloudflare.com");
  process.exit(1);
}

const base = process.env.KEYCLOAK_BASE_URL || "http://localhost:80/auth";
const adminUser = process.env.KEYCLOAK_ADMIN_USER || "admin";
const adminPassword = process.env.KEYCLOAK_ADMIN_PASSWORD || "admin123";
const realm = process.env.KEYCLOAK_REALM || "umbral";
const clientId = process.env.KEYCLOAK_CLIENT_ID || "umbral-frontend";

async function main() {
  // 0. Actualizar KC_HOSTNAME_URL y PublicForwardedHost para que los enlaces usen el túnel
  try {
    const fs = require("fs");
    const path = require("path");
    const root = path.join(__dirname, "..");

    // 0a. docker-compose.yml — KC_HOSTNAME_URL
    let compose = fs.readFileSync(path.join(root, "docker-compose.yml"), "utf8");
    const oldHostname = compose.match(/KC_HOSTNAME_URL: (https?:\/\/[^\s]+)/);
    if (oldHostname) {
      const newHostname = tunnelUrl.replace(/\/$/, "");
      compose = compose.replace(/KC_HOSTNAME_URL: https?:\/\/[^\s]+/, `KC_HOSTNAME_URL: ${newHostname}`);
      fs.writeFileSync(path.join(root, "docker-compose.yml"), compose);
      console.log(`KC_HOSTNAME_URL actualizado: ${oldHostname[1]} -> ${newHostname}`);
    }

    // 0b. docker-compose.prod.yml — KeycloakAdmin__PublicForwardedHost
    let prod = fs.readFileSync(path.join(root, "docker-compose.prod.yml"), "utf8");
    const oldFwd = prod.match(/KeycloakAdmin__PublicForwardedHost:\s*"[^"]+"/);
    if (oldFwd) {
      prod = prod.replace(/KeycloakAdmin__PublicForwardedHost:\s*"[^"]+"/, `KeycloakAdmin__PublicForwardedHost: "${tunnelUrl}"`);
      fs.writeFileSync(path.join(root, "docker-compose.prod.yml"), prod);
      console.log(`PublicForwardedHost actualizado: ${oldFwd[0]} -> ${tunnelUrl}`);
    }
  } catch (e) {
    console.warn("No se pudo actualizar los archivos:", e.message);
  }
  const tokenResp = await fetch(`${base}/realms/master/protocol/openid-connect/token`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: `client_id=admin-cli&username=${encodeURIComponent(adminUser)}&password=${encodeURIComponent(adminPassword)}&grant_type=password`,
  });
  const tokenData = await tokenResp.json();
  if (!tokenData.access_token) {
    console.error("No se pudo autenticar contra Keycloak:", tokenData);
    process.exit(1);
  }
  const token = tokenData.access_token;
  const authHeader = { Authorization: `Bearer ${token}` };

  const listResp = await fetch(`${base}/admin/realms/${realm}/clients?clientId=${clientId}`, { headers: authHeader });
  const clients = await listResp.json();
  if (!clients[0]) {
    console.error(`Client "${clientId}" no encontrado en el realm "${realm}"`);
    process.exit(1);
  }
  const clientUuid = clients[0].id;

  const clientResp = await fetch(`${base}/admin/realms/${realm}/clients/${clientUuid}`, { headers: authHeader });
  const client = await clientResp.json();

  let changed = false;
  const redirect = `${tunnelUrl}/*`;
  if (!client.redirectUris.includes(redirect)) {
    client.redirectUris.push(redirect);
    changed = true;
  }
  if (!client.webOrigins.includes(tunnelUrl)) {
    client.webOrigins.push(tunnelUrl);
    changed = true;
  }
  const plr = client.attributes?.["post.logout.redirect.uris"] || "";
  if (!plr.includes(tunnelUrl)) {
    client.attributes["post.logout.redirect.uris"] = plr ? `${plr}##${tunnelUrl}` : tunnelUrl;
    changed = true;
  }

  if (!changed) {
    console.log(`"${tunnelUrl}" ya estaba registrado — nada que hacer.`);
    return;
  }

  const putResp = await fetch(`${base}/admin/realms/${realm}/clients/${clientUuid}`, {
    method: "PUT",
    headers: { ...authHeader, "Content-Type": "application/json" },
    body: JSON.stringify(client),
  });
  if (putResp.status !== 204) {
    console.error("Falló el PUT:", putResp.status, await putResp.text());
    process.exit(1);
  }
  console.log(`Listo: "${tunnelUrl}" registrado como redirect URI / web origin de "${clientId}".`);

  // 5. Recargar Keycloak para que use el nuevo KC_HOSTNAME_URL
  try {
    const { execSync } = require("child_process");
    console.log("Reiniciando Keycloak para aplicar KC_HOSTNAME_URL...");
    execSync("docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d keycloak", {
      cwd: require("path").join(__dirname, ".."),
      stdio: "inherit",
      timeout: 60000,
    });
  } catch (e) {
    console.warn("No se pudo reiniciar Keycloak. Hacelo manualmente:", e.message);
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
