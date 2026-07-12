import { createContext, useContext, useReducer, type ReactNode } from "react";
import type { GameState, GameAction } from "../types/game";
import type { SessionStatus } from "../types/session";

const initialState: GameState = {
  sessionId: null,
  sessionName: "",
  sessionStatus: null,
  currentMissionType: null,
  currentStageOrder: 0,
  participantStageOrder: 1,
  totalStages: 0,
  stages: [],
  timeLimitSeconds: 0,
  currentQuizId: null,
  elapsedSeconds: 0,
  clues: [],
  score: 0,
  connectionState: "Disconnected",
  ranking: [],
  currentQuestion: null,
  selectedAnswerIndex: null,
  isWaiting: false,
  gatePosition: 0,
  gateThreshold: 0,
};

function gameReducer(state: GameState, action: GameAction): GameState {
  switch (action.type) {
    case "SESSION_LOADED": {
      const currentStage = action.stages.find(s => s.order === action.stageOrder)
        ?? action.stages[0]
        ?? null;
      const missionId = currentStage?.missionId ?? null;
      const missionStages = missionId
        ? action.stages.filter(s => s.missionId === missionId)
        : [];
      const timeMinutes = missionStages[0]?.timeMinutes ?? 0;
      const quizId = currentStage?.quizId ?? null;
      return {
        ...state,
        sessionName: action.name,
        sessionStatus: action.status,
        currentMissionType: action.missionType,
        currentStageOrder: action.stageOrder,
        totalStages: action.totalStages,
        stages: action.stages,
        timeLimitSeconds: timeMinutes * 60,
        currentQuizId: quizId,
        elapsedSeconds: 0,
      };
    }

    case "STAGE_ADVANCED":
      return {
        ...state,
        participantStageOrder: action.participantStageOrder,
        totalStages: action.totalStages,
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
        const next = state.elapsedSeconds + 1;
        if (state.timeLimitSeconds > 0 && next >= state.timeLimitSeconds) {
          return { ...state, elapsedSeconds: state.timeLimitSeconds };
        }
        return { ...state, elapsedSeconds: next };
      }
      return state;

    case "TIME_UP":
      return { ...state, elapsedSeconds: state.timeLimitSeconds };

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
        answersDisabled: false,
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

    case "GATE_REACHED":
      return {
        ...state,
        isWaiting: true,
        gatePosition: action.position,
        gateThreshold: action.threshold,
      };

    case "GATE_OPENED":
      return {
        ...state,
        isWaiting: false,
        gatePosition: 0,
        gateThreshold: 0,
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
