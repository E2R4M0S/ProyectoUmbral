import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { GameContext } from "../../../contexts/GameContext";
import { Timer } from "../Timer";
import type { GameState } from "../../../types/game";
import type { SessionStage } from "../../../types/session";

function createMockDispatch() {
  return vi.fn();
}

function treasureStage(timeMinutes: number, order = 1): SessionStage {
  return {
    missionId: "mission-1",
    missionTitle: "Test Mission",
    stageName: "Stage 1",
    missionType: "Treasure",
    order,
    timeMinutes,
  };
}

function renderTimer(state: Partial<GameState> = {}) {
  const dispatch = createMockDispatch();
  const defaultState: GameState = {
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

  const contextValue = {
    state: { ...defaultState, ...state },
    dispatch,
  };

  const { rerender } = render(
    <GameContext.Provider value={contextValue}>
      <Timer />
    </GameContext.Provider>
  );

  return { dispatch, rerender, contextValue };
}

describe("Timer", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders initial time as 00:00", () => {
    renderTimer({ currentMissionElapsedSeconds: 0, stages: [treasureStage(60)], participantStageOrder: 1, currentMissionType: "Treasure" });
    expect(screen.getByText("60:00")).toBeInTheDocument();
  });

  it("renders elapsed time correctly", () => {
    renderTimer({ currentMissionElapsedSeconds: 65, stages: [treasureStage(60)], participantStageOrder: 1, currentMissionType: "Treasure" });
    expect(screen.getByText("58:55")).toBeInTheDocument();
  });

  it("renders large time values", () => {
    renderTimer({ currentMissionElapsedSeconds: 100, stages: [treasureStage(100)], participantStageOrder: 1, currentMissionType: "Treasure" });
    expect(screen.getByText("98:20")).toBeInTheDocument();
  });

  it("dispatches TICK every second", () => {
    const { dispatch } = renderTimer({ currentMissionElapsedSeconds: 0 });

    vi.advanceTimersByTime(3000);

    expect(dispatch).toHaveBeenCalledTimes(3);
    expect(dispatch).toHaveBeenCalledWith({ type: "TICK" });
  });
});
