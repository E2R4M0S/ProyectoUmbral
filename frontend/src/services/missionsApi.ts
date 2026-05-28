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