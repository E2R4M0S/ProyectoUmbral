import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { GameContext } from "../../../contexts/GameContext";
import { QuestionCard } from "../QuestionCard";
import type { GameState, TriviaQuestion } from "../../../types/game";

vi.mock("../../../services/api", () => ({ fetchWithAuth: vi.fn() }));
vi.mock("../../../auth/keycloak", () => ({
  userManager: { getUser: vi.fn() },
}));

import { fetchWithAuth } from "../../../services/api";
import { userManager } from "../../../auth/keycloak";

const mockFetch = vi.mocked(fetchWithAuth);
const mockGetUser = vi.mocked(userManager.getUser);

const testQuestion: TriviaQuestion = {
  sessionId: "sess-1",
  questionId: "q-1",
  questionText: "What is 2 + 2?",
  options: ["1", "2", "3", "4"],
  timeLimitSeconds: 30,
  askedAt: new Date().toISOString(),
};

function renderCard(stateOverride: Partial<GameState> = {}) {
  const dispatch = vi.fn();
  const state: GameState = {
    sessionId: "sess-1",
    sessionName: "Test",
    sessionStatus: "Active",
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
    connectionState: "Connected",
    ranking: [],
    currentQuestion: null,
    selectedAnswerIndex: null,
    isWaiting: false,
    gatePosition: 0,
    gateThreshold: 0,
    ...stateOverride,
  };
  render(
    <GameContext.Provider value={{ state, dispatch }}>
      <QuestionCard question={testQuestion} />
    </GameContext.Provider>
  );
  return { dispatch };
}

describe("QuestionCard", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
    mockGetUser.mockResolvedValue({
      profile: { sub: "user-1", name: "Player" },
    } as any);
  });

  it("renders the question text and all options", () => {
    renderCard();
    expect(screen.getByText("What is 2 + 2?")).toBeInTheDocument();
    expect(screen.getByText("1")).toBeInTheDocument();
    expect(screen.getByText("4")).toBeInTheDocument();
  });

  it("all option buttons are enabled by default", () => {
    renderCard();
    const buttons = screen.getAllByRole("button");
    buttons.forEach((btn) => expect(btn).not.toBeDisabled());
  });

  it("all option buttons are disabled when answersDisabled is true", () => {
    renderCard({ answersDisabled: true });
    const buttons = screen.getAllByRole("button");
    buttons.forEach((btn) => expect(btn).toBeDisabled());
  });

  it("submits answer and shows correct feedback on success", async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ isCorrect: true, pointsAwarded: 100 }),
    } as Response);

    const { dispatch } = renderCard();
    fireEvent.click(screen.getByText("4"));

    await waitFor(() =>
      expect(screen.getByText("✅ ¡Correcta!")).toBeInTheDocument()
    );
    expect(screen.getByText("+100 pts")).toBeInTheDocument();
    expect(dispatch).toHaveBeenCalledWith({ type: "SET_SCORE", score: 100 });
  });

  it("shows wrong feedback when answer is incorrect", async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ isCorrect: false, pointsAwarded: 0 }),
    } as Response);

    renderCard();
    fireEvent.click(screen.getByText("1"));

    await waitFor(() =>
      expect(screen.getByText("❌ Equivocada")).toBeInTheDocument()
    );
    expect(screen.getByText("0 pts")).toBeInTheDocument();
  });

  it("disables buttons after selecting an answer", async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ isCorrect: true, pointsAwarded: 50 }),
    } as Response);

    renderCard();
    fireEvent.click(screen.getByText("4"));

    await waitFor(() => expect(screen.getByText("✅ ¡Correcta!")).toBeInTheDocument());
    screen.getAllByRole("button").forEach((btn) => expect(btn).toBeDisabled());
  });

  it("shows error message when API call fails", async () => {
    mockFetch.mockResolvedValue({
      ok: false,
      status: 500,
      text: () => Promise.resolve("Server error"),
    } as Response);

    renderCard();
    fireEvent.click(screen.getByText("2"));

    await waitFor(() =>
      expect(screen.getByText("Server error")).toBeInTheDocument()
    );
  });

  it("shows connection error on network failure", async () => {
    mockFetch.mockRejectedValue(new Error("Network failure"));

    renderCard();
    fireEvent.click(screen.getByText("3"));

    await waitFor(() =>
      expect(screen.getByText(/Network failure/i)).toBeInTheDocument()
    );
  });

  it("does not submit if sent is already true (double click)", async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ isCorrect: true, pointsAwarded: 50 }),
    } as Response);

    renderCard();
    fireEvent.click(screen.getByText("4"));
    await waitFor(() => expect(screen.getByText("✅ ¡Correcta!")).toBeInTheDocument());

    fireEvent.click(screen.getByText("1"));

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  it("accumulates score in sessionStorage", async () => {
    sessionStorage.setItem("score_sess-1", "50");
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ isCorrect: true, pointsAwarded: 100 }),
    } as Response);

    const { dispatch } = renderCard();
    fireEvent.click(screen.getByText("4"));

    await waitFor(() => expect(screen.getByText("✅ ¡Correcta!")).toBeInTheDocument());
    expect(dispatch).toHaveBeenCalledWith({ type: "SET_SCORE", score: 150 });
  });
});
