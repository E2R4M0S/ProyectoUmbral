import { fetchWithAuth } from "./api";
import type {
  SessionResponse,
  SessionDetail,
  CreateSessionRequest,
  SessionProgress,
  GetSessionsParams,
  GetSessionsResponse,
  AdvanceStageResponse,
  MySessionItem,
  SessionRankingItem,
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

export async function advanceStage(id: string): Promise<AdvanceStageResponse> {
  const response = await fetchWithAuth(`/api/sessions/${id}/advance-stage`, {
    method: "PATCH",
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

export interface ValidateQrRequest {
  stageId: string;
  token: string;
}

export interface ValidateQrResponse {
  isValid: boolean;
  advanced: boolean;
  currentStageOrder: number;
  totalStages: number;
  isLastStage: boolean;
  isAtGate: boolean;
  gateOpened: boolean;
  gatePosition: number;
  gateThreshold: number;
  isEliminated: boolean;
  errorMessage: string | null;
}

export interface UnifiedRankingEntry {
  position: number;
  userId: string;
  displayName: string;
  totalScore: number;
}

export async function getUnifiedRanking(period: "all" | "monthly"): Promise<UnifiedRankingEntry[]> {
  const response = await fetchWithAuth(`/api/sessions/participants/ranking?period=${period}`);
  if (!response.ok) return [];
  return response.json();
}

// Catálogo del participante — sesiones en las que se ha unido.
export async function getMySessions(): Promise<MySessionItem[]> {
  const response = await fetchWithAuth("/api/sessions/participants/me/sessions");
  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }
  return response.json();
}

export async function getSessionRanking(id: string): Promise<SessionRankingItem[]> {
  const response = await fetchWithAuth(`/api/sessions/${id}/ranking`);
  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }
  return response.json();
}

export async function validateQr(sessionId: string, body: ValidateQrRequest): Promise<ValidateQrResponse> {
  const response = await fetchWithAuth(`/api/sessions/${sessionId}/validate-qr`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}
