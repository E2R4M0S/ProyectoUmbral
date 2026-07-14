import { fetchWithAuth } from "./api";
import type {
  CreateMissionRequest,
  MissionResponse,
  GetMissionsParams,
  GetMissionsResponse,
  MissionDetail,
} from "../types/mission";

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

export async function changeMissionStatus(
  id: string,
  newStatus: string,
): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/missions/${id}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status: newStatus }),
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }
}

export async function createStage(
  missionId: string,
  data: { name: string; description: string; order: number },
): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/missions/${missionId}/stages`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...data, missionId }),
  });
  if (!response.ok) {
    const body = await response.text();
    let msg = body;
    try { const j = JSON.parse(body); msg = j.detail || j.message || j.title || body; } catch {}
    throw new ApiError(response.status, msg);
  }
}

export async function updateStage(
  missionId: string,
  stageId: string,
  data: { name: string; description: string; order: number },
): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/missions/${missionId}/stages/${stageId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...data, missionId, stageId }),
  });
  if (!response.ok) throw new ApiError(response.status, await response.text());
}

export async function createClue(
  missionId: string,
  stageId: string,
  data: { content: string; penalty?: number; releaseType?: string },
): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/missions/${missionId}/stages/${stageId}/clues`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...data, missionId, stageId, releaseType: "Manual" }),
  });
  if (!response.ok) throw new ApiError(response.status, await response.text());
}

export async function deleteClue(
  missionId: string,
  stageId: string,
  clueId: string,
): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/missions/${missionId}/stages/${stageId}/clues/${clueId}`, {
    method: "DELETE",
  });
  if (!response.ok) throw new ApiError(response.status, await response.text());
}

export async function deleteStage(
  missionId: string,
  stageId: string,
): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/missions/${missionId}/stages/${stageId}`, {
    method: "DELETE",
  });
  if (!response.ok) {
    const body = await response.text();
    let msg = body;
    try { const j = JSON.parse(body); msg = j.error || j.message || body; } catch {}
    throw new ApiError(response.status, msg);
  }
}

export async function listMissions(
  params: GetMissionsParams,
): Promise<GetMissionsResponse> {
  const searchParams = new URLSearchParams();
  if (params.search) searchParams.set("search", params.search);
  if (params.difficulty) searchParams.set("difficulty", params.difficulty);
  if (params.status) searchParams.set("status", params.status);
  searchParams.set("page", String(params.page ?? 1));
  searchParams.set("pageSize", String(params.pageSize ?? 10));

  const response = await fetchWithAuth(
    `/api/missions?${searchParams.toString()}`,
    { method: "GET" },
  );

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function getMissionById(id: string): Promise<MissionDetail> {
  const response = await fetchWithAuth(`/api/missions/${id}`, {
    method: "GET",
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export interface UpdateMissionRequest {
  title: string;
  description: string;
  difficulty: "Easy" | "Medium" | "Hard";
  timeMinutes: number;
  type: "Treasure" | "Trivia";
}

export async function updateMission(
  id: string,
  data: UpdateMissionRequest,
): Promise<MissionResponse> {
  const response = await fetchWithAuth(`/api/admin/missions/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  if (response.status === 204) {
    return { id } as MissionResponse;
  }

  return response.json();
}

export async function getActiveMissions(
  params?: Omit<GetMissionsParams, "status">,
): Promise<GetMissionsResponse> {
  const searchParams = new URLSearchParams();
  if (params?.search) searchParams.set("search", params.search);
  if (params?.difficulty) searchParams.set("difficulty", params.difficulty);
  searchParams.set("page", String(params?.page ?? 1));
  searchParams.set("pageSize", String(params?.pageSize ?? 10));

  const response = await fetchWithAuth(
    `/api/missions/active?${searchParams.toString()}`,
    { method: "GET" },
  );

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}