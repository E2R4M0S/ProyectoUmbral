import { fetchWithAuth } from "./api";
import type { CreateMissionRequest, MissionResponse } from "../types/mission";

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: string,
  ) {
    super(`API Error: ${status}`);
    this.name = "ApiError";
  }
}

export async function createMission(
  data: CreateMissionRequest,
): Promise<MissionResponse> {
  const response = await fetchWithAuth("/api/admin/missions", {
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