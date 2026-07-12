import type { SessionStatus } from "./session";

export type ConnectionState = "Connecting" | "Connected" | "Reconnecting" | "Disconnected";

export interface JoinSessionResponse {
  sessionId: string;
  userId: string;
  joinedAt: string;
}

export interface RankingEntry {
  position: number;
  teamName: string;
  score: number;
}

export interface TriviaQuestion {
  sessionId: string;
  questionId: string;
  questionText: string;
  options: string[];
  timeLimitSeconds: number;
  askedAt: string;
}

export interface GameState {
  sessionId: string | null;
  sessionName: string;
  sessionStatus: SessionStatus | null;
  currentMissionType: string | null;
  currentStageOrder: number;
  participantStageOrder: number;
  totalStages: number;
  elapsedSeconds: number;
  clues: unknown[];
  score: number;
  connectionState: ConnectionState;
  answersDisabled?: boolean;
  ranking: RankingEntry[];
  currentQuestion: TriviaQuestion | null;
  selectedAnswerIndex: number | null;
  isWaiting: boolean;
  gatePosition: number;
  gateThreshold: number;
}

export type GameAction =
  | { type: "SESSION_LOADED"; name: string; status: SessionStatus; missionType: string | null; stageOrder: number; totalStages: number }
  | { type: "STAGE_ADVANCED"; participantStageOrder: number; totalStages: number }
  | { type: "STATUS_CHANGED"; status: SessionStatus }
  | { type: "PROGRESS_UPDATED"; data: unknown }
  | { type: "CLUE_RELEASED"; clue: unknown }
  | { type: "CONNECTION_STATE_CHANGED"; state: ConnectionState }
  | { type: "TICK" }
  | { type: "SET_SCORE"; score: number }
  | { type: "RANKING_UPDATED"; ranking: RankingEntry[] }
  | { type: "QUESTION_RECEIVED"; question: TriviaQuestion }
  | { type: "ANSWER_SELECTED"; answerIndex: number }
  | { type: "QUESTION_CLEARED" }
  | { type: "GATE_REACHED"; position: number; threshold: number }
  | { type: "GATE_OPENED" };
