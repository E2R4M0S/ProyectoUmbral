export type Difficulty = "Easy" | "Medium" | "Hard";
export type MissionType = "Treasure" | "Trivia";

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