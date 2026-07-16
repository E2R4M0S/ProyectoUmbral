import type { SessionStatus, SessionStage } from "./session";

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
  userId?: string;
}

export interface MyTeam {
  id: string;
  name: string;
  memberIds: string[];
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
  stages: SessionStage[];
  currentQuizId: string | null;
  // Session-wide elapsed seconds since it started (server-synced) — used for "total time
  // played" stats, not for the mission countdown (see currentMissionElapsedSeconds for that).
  elapsedSeconds: number;
  // Seconds since the CURRENT mission actually started (server-anchored), server-synced. This
  // is what both the operator's dashboard and every participant's Timer count down against, so
  // everyone sees the exact same remaining time regardless of how long prior missions actually took.
  currentMissionElapsedSeconds: number;
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
  myUserId: string | null;
  myTeam: MyTeam | null;
}

export type GameAction =
  | { type: "SESSION_LOADED"; name: string; status: SessionStatus; missionType: string | null; stageOrder: number; totalStages: number; stages: SessionStage[] }
  | { type: "STAGE_ADVANCED"; participantStageOrder: number; totalStages: number }
  | { type: "STATUS_CHANGED"; status: SessionStatus }
  | { type: "PROGRESS_UPDATED"; data: unknown }
  | { type: "CLUE_RELEASED"; clue: unknown }
  | { type: "CONNECTION_STATE_CHANGED"; state: ConnectionState }
  | { type: "TICK" }
  | { type: "ELAPSED_SYNCED"; elapsedSeconds: number; currentMissionElapsedSeconds: number }
  | { type: "SET_SCORE"; score: number }
  | { type: "RANKING_UPDATED"; ranking: RankingEntry[] }
  | { type: "QUESTION_RECEIVED"; question: TriviaQuestion }
  | { type: "ANSWER_SELECTED"; answerIndex: number }
  | { type: "QUESTION_CLEARED" }
  | { type: "GATE_REACHED"; position: number; threshold: number }
  | { type: "GATE_OPENED" }
  | { type: "MY_IDENTITY_LOADED"; userId: string; team: MyTeam | null };
