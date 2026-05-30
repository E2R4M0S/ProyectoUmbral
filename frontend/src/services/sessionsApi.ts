import { fetchWithAuth } from "./api";
import type {
  SessionResponse,
  SessionDetail,
  CreateSessionRequest,
  SessionProgress,
  GetSessionsParams,
  GetSessionsResponse,
} from "../types/session";
import type { JoinSessionResponse } from "../types/game";

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

export async function joinSession(pin: string): Promise<JoinSessionResponse> {
  const response = await fetchWithAuth("/api/sessions/join", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ pin }),
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
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

export async function transitionSession(id: string, newStatus: string): Promise<void> {
  const response = await fetchWithAuth(`/api/sessions/${id}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ newStatus }),
  });
  if (!response.ok) throw new ApiError(response.status, await response.text());
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

export async function createSession(data: CreateSessionRequest): Promise<SessionResponse> {
  const response = await fetchWithAuth("/api/sessions", {
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

export async function listSessions(params: GetSessionsParams): Promise<GetSessionsResponse> {
  const searchParams = new URLSearchParams();
  if (params.search) searchParams.set("search", params.search);
  if (params.status) searchParams.set("status", params.status);
  searchParams.set("page", String(params.page ?? 1));
  searchParams.set("pageSize", String(params.pageSize ?? 10));

  const response = await fetchWithAuth(
    `/api/sessions?${searchParams.toString()}`,
    { method: "GET" },
  );

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function getSessionById(id: string): Promise<SessionDetail> {
  const response = await fetchWithAuth(`/api/sessions/${id}`, {
    method: "GET",
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function getSessionProgress(id: string): Promise<SessionProgress> {
  const response = await fetchWithAuth(`/api/sessions/${id}/progress`, {
    method: "GET",
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}
