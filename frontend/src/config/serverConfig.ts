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

export function buildApiBase(): string {
  if (!isProductionApk) return "";
  const host = getServerHost();
  return host ? `http://${host}:5000` : "";
}

export function buildKeycloakBase(): string {
  if (!isProductionApk) {
    const hostname = window.location.hostname;
    return hostname === "localhost" ? "http://localhost:8080" : `http://${hostname}:8080`;
  }
  const host = getServerHost();
  return host ? `http://${host}:8080` : "http://localhost:8080";
}

export function buildHubUrl(): string {
  if (!isProductionApk) return "/hub/game";
  const host = getServerHost();
  return host ? `http://${host}:5005/hub/game` : "/hub/game";
}

export function needsServerSetup(): boolean {
  return isProductionApk && !getServerHost();
}
