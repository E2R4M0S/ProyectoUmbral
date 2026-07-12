import { CapacitorHttp } from "@capacitor/core";
import { userManager } from "../auth/keycloak";
import { buildApiBase, isProductionApk } from "../config/serverConfig";

function getApiBase(): string {
  return buildApiBase();
}

function wrapNativeResponse(status: number, data: unknown): Response {
  const body = typeof data === "string" ? data : JSON.stringify(data ?? "");
  return {
    ok: status >= 200 && status < 300,
    status,
    text: () => Promise.resolve(body),
    json: () => Promise.resolve(typeof data === "string" ? JSON.parse(data) : data),
  } as unknown as Response;
}

async function nativeFetch(
  method: string,
  url: string,
  headers: Record<string, string>,
  body?: BodyInit,
): Promise<Response> {
  let data: unknown;
  if (body !== undefined) {
    const ct = headers["Content-Type"] ?? headers["content-type"] ?? "";
    data = ct.includes("application/json") && typeof body === "string"
      ? JSON.parse(body)
      : body;
  }
  const res = await CapacitorHttp.request({ method, url, headers, data });
  return wrapNativeResponse(res.status, res.data);
}

export async function fetchWithAuth(
  url: string,
  options: RequestInit = {},
): Promise<Response> {
  const user = await userManager.getUser();
  const fullUrl = `${getApiBase()}${url}`;
  const method = (options.method as string | undefined) ?? "GET";

  // Build a plain headers object (works for both fetch and CapacitorHttp)
  const headers: Record<string, string> = {};
  if (options.headers) {
    if (options.headers instanceof Headers) {
      options.headers.forEach((v, k) => { headers[k] = v; });
    } else if (Array.isArray(options.headers)) {
      for (const [k, v] of options.headers as [string, string][]) headers[k] = v;
    } else {
      Object.assign(headers, options.headers as Record<string, string>);
    }
  }
  if (user?.access_token) headers["Authorization"] = `Bearer ${user.access_token}`;

  // Native APK: use CapacitorHttp (Java-level, no WebView cross-origin restrictions)
  if (isProductionApk) {
    const response = await nativeFetch(method, fullUrl, headers, options.body as BodyInit);
    if (response.status === 401 && user?.refresh_token) {
      try {
        const refreshed = await userManager.signinSilent();
        if (refreshed?.access_token) {
          headers["Authorization"] = `Bearer ${refreshed.access_token}`;
          return nativeFetch(method, fullUrl, headers, options.body as BodyInit);
        }
      } catch {
        userManager.signinRedirect();
      }
    }
    return response;
  }

  // Web (browser): standard fetch
  const fetchHeaders = new Headers(headers);
  const response = await fetch(fullUrl, { ...options, headers: fetchHeaders });
  if (response.status === 401 && user?.refresh_token) {
    try {
      const refreshed = await userManager.signinSilent();
      if (refreshed?.access_token) {
        fetchHeaders.set("Authorization", `Bearer ${refreshed.access_token}`);
        return fetch(fullUrl, { ...options, headers: fetchHeaders });
      }
    } catch {
      userManager.signinRedirect();
    }
  }
  return response;
}
