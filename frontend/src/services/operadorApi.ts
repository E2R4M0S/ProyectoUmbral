import { fetchWithAuth } from "./api";
import type { CreateOperadorRequest, OperadorResponse } from "../types/operador";

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: string,
  ) {
    super(`API Error: ${status}`);
    this.name = "ApiError";
  }
}

export async function crearOperador(
  data: CreateOperadorRequest,
): Promise<OperadorResponse> {
  const response = await fetchWithAuth("/api/admin/operators", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}
