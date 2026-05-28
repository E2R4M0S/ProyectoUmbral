export type Difficulty = "Easy" | "Medium" | "Hard";
export type MissionType = "Treasure" | "Trivia";
export type MissionStatus = "Draft" | "Active";

export interface CreateMissionRequest {
  title: string;
  description: string;
  difficulty: Difficulty;
  timeMinutes: number;
  type: MissionType;
}

export interface MissionResponse {
  id: string;
  title: string;
  description: string;
  difficulty: Difficulty;
  timeMinutes: number;
  type: MissionType;
  status: string;
  createdAt: string;
}

export interface MissionListItem {
  id: string;
  title: string;
  difficulty: string;
  type: string;
  status: string;
}

export interface MissionDetail {
  id: string;
  title: string;
  description: string;
  difficulty: string;
  timeMinutes: number;
  type: string;
  status: string;
}

export interface GetMissionsParams {
  search?: string;
  difficulty?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export interface GetMissionsResponse {
  items: MissionListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}