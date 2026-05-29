import type { SessionStatus } from "./session";

export type ConnectionState = "Connecting" | "Connected" | "Reconnecting" | "Disconnected";

export interface JoinSessionResponse {
  sessionId: string;
  userId: string;
  joinedAt: string;
}

export interface GameState {
  sessionId: string | null;
  sessionName: string;
  sessionStatus: SessionStatus | null;
  elapsedSeconds: number;
  clues: unknown[];
  score: number;
  connectionState: ConnectionState;
}

export type GameAction =
  | { type: "SESSION_LOADED"; name: string; status: SessionStatus }
  | { type: "STATUS_CHANGED"; status: SessionStatus }
  | { type: "PROGRESS_UPDATED"; data: unknown }
  | { type: "CLUE_RELEASED"; clue: unknown }
  | { type: "CONNECTION_STATE_CHANGED"; state: ConnectionState }
  | { type: "TICK" }
  | { type: "SET_SCORE"; score: number };
