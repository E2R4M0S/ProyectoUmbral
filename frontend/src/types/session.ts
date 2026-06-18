import type { MissionType } from "./mission";

export type SessionStatus = "Scheduled" | "Preparing" | "Active" | "Paused" | "Finished" | "Cancelled";

export interface SessionStage {
  missionId: string;
  missionTitle: string;
  missionType: MissionType | string;
  order: number;
}

export interface SessionResponse {
  id: string;
  name: string;
  pin: string;
  status: SessionStatus;
  currentStageOrder: number;
  stages: SessionStage[];
  startedAt: string | null;
  finishedAt: string | null;
  createdAt: string;
}

export interface SessionListItem {
  id: string;
  name: string;
  missionTitle: string;
  missionType: string;
  currentStageOrder: number;
  stageCount: number;
  status: SessionStatus;
  pin: string;
  participantCount: number;
  createdAt: string;
}

export interface SessionDetail {
  id: string;
  name: string;
  pin: string;
  status: SessionStatus;
  currentStageOrder: number;
  stages: SessionStage[];
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

export interface StageInput {
  missionId: string;
  missionTitle: string;
  missionType: string;
  order: number;
}

export interface CreateSessionRequest {
  name: string;
  stages: StageInput[];
}

export interface AdvanceStageResponse {
  currentStageOrder: number;
  totalStages: number;
  isLastStage: boolean;
}

export interface SessionProgress {
  sessionId: string;
  name: string;
  status: SessionStatus;
  elapsedSeconds: number;
  participants: ParticipantProgress[];
  teamId: string | null;
  teamName: string | null;
}

export interface ParticipantProgress {
  userId: string;
  userAlias: string;
  joinedAt: string;
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
