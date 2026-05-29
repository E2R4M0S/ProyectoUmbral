import { fetchWithAuth } from "./api";

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: string,
  ) {
    super(`API Error: ${status}`);
    this.name = "ApiError";
  }
}

export interface StartSessionResponse {
  id: string;
  status: string;
}

export async function startSession(id: string): Promise<StartSessionResponse> {
  const response = await fetchWithAuth(`/api/sessions/${id}/start`, {
    method: "POST",
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function finishSession(id: string): Promise<StartSessionResponse> {
  const response = await fetchWithAuth(`/api/sessions/${id}/finish`, {
    method: "POST",
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}
