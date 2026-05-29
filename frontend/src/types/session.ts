export type SessionStatus = "Waiting" | "InProgress" | "Finished" | "Cancelled";

export interface SessionResponse {
  id: string;
  name: string;
  missionId: string;
  pin: string;
  status: SessionStatus;
  startedAt: string | null;
  finishedAt: string | null;
  createdAt: string;
}

export interface SessionListItem {
  id: string;
  name: string;
  missionTitle: string;
  status: SessionStatus;
  pin: string;
  createdAt: string;
}

export interface SessionDetail {
  id: string;
  name: string;
  missionId: string;
  missionTitle: string;
  pin: string;
  status: SessionStatus;
  teamId: string | null;
  teamName: string | null;
  participants: SessionParticipant[];
  startedAt: string | null;
  finishedAt: string | null;
  createdAt: string;
}

export interface SessionParticipant {
  id: string;
  userId: string;
  name: string;
  email: string;
  score: number;
}

export interface CreateSessionRequest {
  name: string;
  missionId: string;
}

export interface SessionProgress {
  sessionId: string;
  status: SessionStatus;
  elapsedSeconds: number;
  participants: number;
  teamId: string | null;
  teamName: string | null;
}

export interface GetSessionsParams {
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export interface GetSessionsResponse {
  items: SessionListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}