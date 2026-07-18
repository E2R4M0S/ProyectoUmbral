import { Capacitor } from "@capacitor/core";

const STORAGE_KEY = "umbral_server_host";

// True when running inside the Capacitor Android/iOS shell (production APK).
// More reliable than checking window.location.origin, which can vary by Capacitor version.
export const isProductionApk = Capacitor.isNativePlatform();

export function getServerHost(): string {
  return localStorage.getItem(STORAGE_KEY) ?? "";
}

export function setServerHost(host: string): void {
  localStorage.setItem(STORAGE_KEY, host.trim().replace(/\/$/, ""));
}

export function clearServerHost(): void {
  localStorage.removeItem(STORAGE_KEY);
}

// Two shapes are accepted in the "Configurar servidor" field:
//   - a bare LAN IP/hostname (ej. "192.168.0.3") -> hits each service's direct
//     port, only reachable when the phone is on the same network as the PC.
//   - a full URL (ej. "https://xxxx.trycloudflare.com") -> a single public
//     origin (Caddy behind a Cloudflare Tunnel) that path-routes to every
//     service, same as the browser SPA already does via /api, /hub and /auth.
function isTunnelHost(host: string): boolean {
  return /^https?:\/\//i.test(host);
}

export function buildApiBase(): string {
  if (!isProductionApk) return "";
  const host = getServerHost();
  if (!host) return "";
  return isTunnelHost(host) ? host : `http://${host}:5000`;
}

export function buildKeycloakBase(): string {
  if (!isProductionApk) {
    const hostname = window.location.hostname;
    // Si el frontend se accede por HTTPS (túnel/dominio externo), Keycloak está
    // bajo /auth en el mismo origen (vía Caddy). Si es localhost, Keycloak está
    // en el puerto directo :8080.
    if (window.location.protocol === "https:" || (!hostname.includes("localhost") && !hostname.match(/^\d+\.\d+\.\d+\.\d+$/))) {
      return `${window.location.origin}/auth`;
    }
    return hostname === "localhost" ? "http://localhost:8080" : `http://${hostname}:8080`;
  }
  const host = getServerHost();
  if (!host) return "http://localhost:8080";
  return isTunnelHost(host) ? `${host}/auth` : `http://${host}:8080`;
}

export function buildHubUrl(): string {
  if (!isProductionApk) return "/hub/game";
  const host = getServerHost();
  if (!host) return "/hub/game";
  return isTunnelHost(host) ? `${host}/hub/game` : `http://${host}:5005/hub/game`;
}

export function needsServerSetup(): boolean {
  return isProductionApk && !getServerHost();
}
