import { createContext, useContext, useReducer, type ReactNode } from "react";
import type { GameState, GameAction } from "../types/game";
import type { SessionStatus } from "../types/session";

const initialState: GameState = {
  sessionId: null,
  sessionName: "",
  sessionStatus: null,
  elapsedSeconds: 0,
  clues: [],
  score: 0,
  connectionState: "Disconnected",
  ranking: [],
  currentQuestion: null,
  selectedAnswerIndex: null,
};

function gameReducer(state: GameState, action: GameAction): GameState {
  switch (action.type) {
    case "SESSION_LOADED":
      return {
        ...state,
        sessionName: action.name,
        sessionStatus: action.status,
      };

    case "STATUS_CHANGED":
      return {
        ...state,
        sessionStatus: action.status as SessionStatus,
      };

    case "PROGRESS_UPDATED": {
      const data = action.data;
      if (typeof data === "object" && data !== null && "elapsedSeconds" in data) {
        return {
          ...state,
          elapsedSeconds: (data as { elapsedSeconds: number }).elapsedSeconds,
        };
      }
      return state;
    }

    case "CLUE_RELEASED":
      return {
        ...state,
        clues: [...state.clues, action.clue],
      };

    case "CONNECTION_STATE_CHANGED":
      return {
        ...state,
        connectionState: action.state,
      };

    case "TICK":
      if (state.sessionStatus === "Active") {
        return {
          ...state,
          elapsedSeconds: state.elapsedSeconds + 1,
        };
      }
      return state;

    case "SET_SCORE":
      return {
        ...state,
        score: action.score,
      };

    case "RANKING_UPDATED":
      return {
        ...state,
        ranking: action.ranking,
      };

    case "QUESTION_RECEIVED":
      return {
        ...state,
        currentQuestion: action.question,
        selectedAnswerIndex: null,
      };

    case "ANSWER_SELECTED":
      return {
        ...state,
        selectedAnswerIndex: action.answerIndex,
      };

    case "QUESTION_CLEARED":
      return {
        ...state,
        currentQuestion: null,
        selectedAnswerIndex: null,
      };

    default:
      return state;
  }
}

interface GameContextValue {
  state: GameState;
  dispatch: React.Dispatch<GameAction>;
}

const GameContext = createContext<GameContextValue | null>(null);

function GameProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(gameReducer, initialState);

  return (
    <GameContext.Provider value={{ state, dispatch }}>
      {children}
    </GameContext.Provider>
  );
}

function useGame(): GameContextValue {
  const ctx = useContext(GameContext);
  if (!ctx) {
    throw new Error("useGame must be used within a GameProvider");
  }
  return ctx;
}

export { GameProvider, useGame, GameContext };
