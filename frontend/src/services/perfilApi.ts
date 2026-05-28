import { fetchWithAuth } from "./api";
import type { PerfilData, UpdatePerfilRequest } from "../types/perfil";

/**
 * Obtiene el perfil del participante autenticado.
 * GET /api/teams/profile
 */
export async function getProfile(): Promise<PerfilData> {
  const response = await fetchWithAuth("/api/teams/profile");

  if (!response.ok) {
    const body = await response.text().catch(() => "");
    throw new ApiError(response.status, body);
  }

  return response.json() as Promise<PerfilData>;
}

/**
 * Actualiza el perfil del participante autenticado.
 * PUT /api/teams/profile
 */
export async function updateProfile(
  data: UpdatePerfilRequest,
): Promise<PerfilData> {
  const response = await fetchWithAuth("/api/teams/profile", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const body = await response.text().catch(() => "");
    throw new ApiError(response.status, body);
  }

  return response.json() as Promise<PerfilData>;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: string,
  ) {
    super(`API Error: ${status}`);
    this.name = "ApiError";
  }
}
