import { userManager } from "../auth/keycloak";

const API_BASE_URL = "";

export async function fetchWithAuth(
  url: string,
  options: RequestInit = {},
): Promise<Response> {
  const user = await userManager.getUser();
  const headers = new Headers(options.headers);

  if (user?.access_token) {
    headers.set("Authorization", `Bearer ${user.access_token}`);
  }

  const response = await fetch(`${API_BASE_URL}${url}`, {
    ...options,
    headers,
  });

  if (response.status === 401 && user?.refresh_token) {
    try {
      const refreshedUser = await userManager.signinSilent();
      if (refreshedUser?.access_token) {
        headers.set("Authorization", `Bearer ${refreshedUser.access_token}`);
        return fetch(`${API_BASE_URL}${url}`, {
          ...options,
          headers,
        });
      }
    } catch {
      userManager.signinRedirect({
        extraQueryParams: { audience: "umbral-gateway" },
      });
    }
  }

  return response;
}
