import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { GameContext } from "../../../contexts/GameContext";
import { Timer } from "../Timer";
import type { GameState } from "../../../types/game";

function createMockDispatch() {
  return vi.fn();
}

function renderTimer(state: Partial<GameState> = {}) {
  const dispatch = createMockDispatch();
  const defaultState: GameState = {
    sessionId: null,
    sessionName: "",
    sessionStatus: null,
    elapsedSeconds: 0,
    clues: [],
    score: 0,
    connectionState: "Disconnected",
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
    renderTimer({ elapsedSeconds: 0 });
    expect(screen.getByText("00:00")).toBeInTheDocument();
  });

  it("renders elapsed time correctly", () => {
    renderTimer({ elapsedSeconds: 65 });
    expect(screen.getByText("01:05")).toBeInTheDocument();
  });

  it("renders large time values", () => {
    renderTimer({ elapsedSeconds: 3661 });
    expect(screen.getByText("61:01")).toBeInTheDocument();
  });

  it("dispatches TICK every second", () => {
    const { dispatch } = renderTimer({ elapsedSeconds: 0 });

    vi.advanceTimersByTime(3000);

    expect(dispatch).toHaveBeenCalledTimes(3);
    expect(dispatch).toHaveBeenCalledWith({ type: "TICK" });
  });
});
