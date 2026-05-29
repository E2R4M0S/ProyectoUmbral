import { fetchWithAuth } from "./api";
import type {
  TeamResponse,
  TeamDetail,
  CreateTeamRequest,
  UpdateTeamRequest,
  GetTeamsParams,
  GetTeamsResponse,
} from "../types/team";

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: string,
  ) {
    super(`API Error: ${status}`);
    this.name = "ApiError";
  }
}

export async function createTeam(data: CreateTeamRequest): Promise<TeamResponse> {
  const response = await fetchWithAuth("/api/teams", {
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

export async function listTeams(params: GetTeamsParams): Promise<GetTeamsResponse> {
  const searchParams = new URLSearchParams();
  if (params.search) searchParams.set("search", params.search);
  searchParams.set("page", String(params.page ?? 1));
  searchParams.set("pageSize", String(params.pageSize ?? 10));

  const response = await fetchWithAuth(
    `/api/teams?${searchParams.toString()}`,
    { method: "GET" },
  );

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function getTeamById(id: string): Promise<TeamDetail> {
  const response = await fetchWithAuth(`/api/teams/${id}`, {
    method: "GET",
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function updateTeam(id: string, data: UpdateTeamRequest): Promise<TeamResponse> {
  const response = await fetchWithAuth(`/api/teams/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function joinTeam(joinCode: string): Promise<{ success: boolean }> {
  const response = await fetchWithAuth("/api/teams/join", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ joinCode }),
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function removeMember(teamId: string, memberId: string): Promise<void> {
  const response = await fetchWithAuth(`/api/teams/${teamId}/members/${memberId}`, {
    method: "DELETE",
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }
}