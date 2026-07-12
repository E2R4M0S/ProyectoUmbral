import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, act } from "@testing-library/react";
import { GameContext } from "../../../contexts/GameContext";
import { CountdownTimer } from "../CountdownTimer";
import type { GameState } from "../../../types/game";

function renderTimer(timeLimitSeconds: number, overrides: Partial<GameState> = {}, onExpired?: () => void) {
  const defaultState: GameState = {
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
    ...overrides,
  };

  const dispatch = vi.fn();

  return render(
    <GameContext.Provider value={{ state: defaultState, dispatch }}>
      <CountdownTimer timeLimitSeconds={timeLimitSeconds} onExpired={onExpired} />
    </GameContext.Provider>
  );
}

describe("CountdownTimer", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders the initial time padded to 2 digits", () => {
    renderTimer(30);
    expect(screen.getByText("30s")).toBeInTheDocument();
  });

  it("renders single-digit seconds padded with leading zero", () => {
    renderTimer(5);
    expect(screen.getByText("05s")).toBeInTheDocument();
  });

  it("counts down by 1 each second", () => {
    renderTimer(10);
    expect(screen.getByText("10s")).toBeInTheDocument();

    act(() => { vi.advanceTimersByTime(1000); });
    expect(screen.getByText("09s")).toBeInTheDocument();

    act(() => { vi.advanceTimersByTime(1000); });
    expect(screen.getByText("08s")).toBeInTheDocument();
  });

  it("stops at 00 and does not go negative", () => {
    renderTimer(2);

    act(() => { vi.advanceTimersByTime(3000); });
    expect(screen.getByText("00s")).toBeInTheDocument();
  });

  it("calls onExpired when timer reaches zero", () => {
    const onExpired = vi.fn();
    renderTimer(2, {}, onExpired);

    act(() => { vi.advanceTimersByTime(2000); });

    expect(onExpired).toHaveBeenCalledTimes(1);
  });

  it("does not call onExpired before timer runs out", () => {
    const onExpired = vi.fn();
    renderTimer(5, {}, onExpired);

    act(() => { vi.advanceTimersByTime(3000); });

    expect(onExpired).not.toHaveBeenCalled();
  });

  it("onExpired is optional — does not throw when omitted", () => {
    renderTimer(1);
    expect(() => {
      act(() => { vi.advanceTimersByTime(1000); });
    }).not.toThrow();
  });

  it("renders countdown-timer container element", () => {
    renderTimer(10);
    expect(document.querySelector(".countdown-timer")).toBeInTheDocument();
  });

  it("renders countdown-display element", () => {
    renderTimer(15);
    expect(document.querySelector(".countdown-display")).toBeInTheDocument();
  });
});
