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
  currentQuizId: null,
  elapsedSeconds: 0,
  currentMissionElapsedSeconds: 0,
  clues: [],
  score: 0,
  connectionState: "Disconnected",
  ranking: [],
  currentQuestion: null,
  selectedAnswerIndex: null,
  isWaiting: false,
  gatePosition: 0,
  gateThreshold: 0,
  myUserId: null,
  myTeam: null,
};

function gameReducer(state: GameState, action: GameAction): GameState {
  switch (action.type) {
    case "SESSION_LOADED": {
      // Backend stores currentStageOrder as 0-based index; stage.order is 1-based (starts at 1).
      // session.GetCurrentStage() uses currentStageOrder + 1, so we must do the same here.
      const currentStage = action.stages.find(s => s.order === action.stageOrder + 1)
        ?? action.stages[0]
        ?? null;
      const quizId = currentStage?.quizId ?? null;
      // When the session advances to a new stage (e.g. Trivia ends → Treasure starts),
      // ensure the participant's local stage pointer also advances.
      const newParticipantStageOrder = Math.max(state.participantStageOrder, action.stageOrder + 1);
      return {
        ...state,
        sessionName: action.name,
        sessionStatus: action.status,
        currentMissionType: currentStage?.missionType ?? null,
        currentStageOrder: action.stageOrder,
        participantStageOrder: newParticipantStageOrder,
        totalStages: action.totalStages,
        stages: action.stages,
        currentQuizId: quizId,
      };
    }

    case "STAGE_ADVANCED": {
      const newStage = state.stages.find(s => s.order === action.participantStageOrder);
      return {
        ...state,
        participantStageOrder: action.participantStageOrder,
        totalStages: action.totalStages,
        currentMissionType: newStage?.missionType ?? state.currentMissionType,
      };
    }

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
      // Both counters are server-anchored absolute elapsed times — tick up every second
      // locally between syncs, same as the operator dashboard's local clock.
      if (state.sessionStatus === "Active") {
        return {
          ...state,
          elapsedSeconds: state.elapsedSeconds + 1,
          currentMissionElapsedSeconds: state.currentMissionElapsedSeconds + 1,
        };
      }
      return state;

    case "ELAPSED_SYNCED":
      return {
        ...state,
        elapsedSeconds: action.elapsedSeconds,
        currentMissionElapsedSeconds: action.currentMissionElapsedSeconds,
      };

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

    case "MY_IDENTITY_LOADED":
      return {
        ...state,
        myUserId: action.userId,
        myTeam: action.team,
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
