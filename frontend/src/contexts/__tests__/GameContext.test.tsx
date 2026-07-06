import { describe, it, expect } from "vitest";
import { renderHook, act } from "@testing-library/react";
import type { ReactNode } from "react";
import { GameProvider, useGame } from "../GameContext";
import type { TriviaQuestion, RankingEntry } from "../../types/game";

function wrapper({ children }: { children: ReactNode }) {
  return <GameProvider>{children}</GameProvider>;
}

const mockQuestion: TriviaQuestion = {
  sessionId: "s1",
  questionId: "q1",
  questionText: "¿Cuál es la capital?",
  options: ["Lima", "Bogotá", "Caracas"],
  timeLimitSeconds: 30,
  askedAt: new Date().toISOString(),
};

describe("GameContext reducer", () => {
  it("has correct initial state", () => {
    const { result } = renderHook(() => useGame(), { wrapper });
    const { state } = result.current;

    expect(state.sessionId).toBeNull();
    expect(state.sessionStatus).toBeNull();
    expect(state.elapsedSeconds).toBe(0);
    expect(state.score).toBe(0);
    expect(state.clues).toHaveLength(0);
    expect(state.ranking).toHaveLength(0);
    expect(state.currentQuestion).toBeNull();
    expect(state.connectionState).toBe("Disconnected");
  });

  it("SESSION_LOADED sets name and status", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "SESSION_LOADED", name: "Misión Alpha", status: "Active" });
    });

    expect(result.current.state.sessionName).toBe("Misión Alpha");
    expect(result.current.state.sessionStatus).toBe("Active");
  });

  it("STATUS_CHANGED updates sessionStatus", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "STATUS_CHANGED", status: "Paused" });
    });

    expect(result.current.state.sessionStatus).toBe("Paused");
  });

  it("CLUE_RELEASED appends clue to list", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "CLUE_RELEASED", clue: { text: "Busca en el norte" } });
      result.current.dispatch({ type: "CLUE_RELEASED", clue: { text: "Segunda pista" } });
    });

    expect(result.current.state.clues).toHaveLength(2);
  });

  it("TICK increments elapsedSeconds only when Active", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "STATUS_CHANGED", status: "Active" });
      result.current.dispatch({ type: "TICK" });
      result.current.dispatch({ type: "TICK" });
    });

    expect(result.current.state.elapsedSeconds).toBe(2);
  });

  it("TICK does not increment when not Active", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "STATUS_CHANGED", status: "Paused" });
      result.current.dispatch({ type: "TICK" });
      result.current.dispatch({ type: "TICK" });
    });

    expect(result.current.state.elapsedSeconds).toBe(0);
  });

  it("SET_SCORE updates score", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "SET_SCORE", score: 150 });
    });

    expect(result.current.state.score).toBe(150);
  });

  it("RANKING_UPDATED replaces ranking list", () => {
    const ranking: RankingEntry[] = [
      { position: 1, teamName: "Equipo A", score: 500 },
      { position: 2, teamName: "Equipo B", score: 300 },
    ];

    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "RANKING_UPDATED", ranking });
    });

    expect(result.current.state.ranking).toHaveLength(2);
    expect(result.current.state.ranking[0].teamName).toBe("Equipo A");
  });

  it("QUESTION_RECEIVED sets current question and resets answer state", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "ANSWER_SELECTED", answerIndex: 2 });
      result.current.dispatch({ type: "QUESTION_RECEIVED", question: mockQuestion });
    });

    expect(result.current.state.currentQuestion).toBe(mockQuestion);
    expect(result.current.state.selectedAnswerIndex).toBeNull();
    expect(result.current.state.answersDisabled).toBe(false);
  });

  it("ANSWER_SELECTED stores selected index", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "ANSWER_SELECTED", answerIndex: 1 });
    });

    expect(result.current.state.selectedAnswerIndex).toBe(1);
  });

  it("QUESTION_CLEARED resets currentQuestion and selectedAnswerIndex", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "QUESTION_RECEIVED", question: mockQuestion });
      result.current.dispatch({ type: "QUESTION_CLEARED" });
    });

    expect(result.current.state.currentQuestion).toBeNull();
    expect(result.current.state.selectedAnswerIndex).toBeNull();
  });

  it("CONNECTION_STATE_CHANGED updates connectionState", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "CONNECTION_STATE_CHANGED", state: "Connected" });
    });

    expect(result.current.state.connectionState).toBe("Connected");
  });

  it("PROGRESS_UPDATED with elapsedSeconds updates elapsed time", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "PROGRESS_UPDATED", data: { elapsedSeconds: 42 } });
    });

    expect(result.current.state.elapsedSeconds).toBe(42);
  });

  it("PROGRESS_UPDATED without elapsedSeconds does not change state", () => {
    const { result } = renderHook(() => useGame(), { wrapper });

    act(() => {
      result.current.dispatch({ type: "PROGRESS_UPDATED", data: { someOtherField: "x" } });
    });

    expect(result.current.state.elapsedSeconds).toBe(0);
  });

  it("useGame throws when used outside GameProvider", () => {
    expect(() => {
      renderHook(() => useGame());
    }).toThrow("useGame must be used within a GameProvider");
  });
});
