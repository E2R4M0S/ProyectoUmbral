import { fetchWithAuth } from "./api";
import type { PerfilData, UpdatePerfilRequest } from "../types/perfil";

export async function getProfile(): Promise<PerfilData> {
  const response = await fetchWithAuth("/api/profile");

  if (!response.ok) {
    const body = await response.text().catch(() => "");
    throw new ApiError(response.status, body);
  }

  return response.json() as Promise<PerfilData>;
}

export async function updateProfile(
  data: UpdatePerfilRequest,
): Promise<PerfilData> {
  const response = await fetchWithAuth("/api/profile", {
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
