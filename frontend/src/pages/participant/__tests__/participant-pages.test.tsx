import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import { GameContext } from "../../../contexts/GameContext";
import type { GameState } from "../../../types/game";

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock("react-oidc-context", () => ({
  useAuth: () => ({ user: { profile: { sub: "u1", name: "Player" } } }),
}));

vi.mock("../../../services/sessionsApi", () => ({
  joinSession: vi.fn(),
  getSessionById: vi.fn(),
  startSession: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/teamsApi", () => ({
  joinTeam: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/perfilApi", () => ({
  getProfile: vi.fn(),
  updateProfile: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../hooks/useSignalR", () => ({
  useSignalR: vi.fn(),
}));

// Mock child game components to avoid cascading useGame issues
vi.mock("../../../components/game/Timer", () => ({ Timer: () => <span>Timer</span> }));
vi.mock("../../../components/game/ClueCard", () => ({ ClueCard: () => null }));
vi.mock("../../../components/game/RankingBoard", () => ({ RankingBoard: () => <div>Ranking</div> }));
vi.mock("../../../components/game/QuestionCard", () => ({ QuestionCard: () => <div>Question</div> }));

// ── Imports ──────────────────────────────────────────────────────────────────
import { joinSession } from "../../../services/sessionsApi";
import { joinTeam } from "../../../services/teamsApi";
import { getProfile } from "../../../services/perfilApi";

import { UnirseSesion } from "../UnirseSesion";
import { UnirseEquipo } from "../UnirseEquipo";
import { MiPerfil } from "../MiPerfil";
import { NextStageRedirect } from "../NextStageRedirect";
import { WaitingRoom } from "../game/WaitingRoom";
import { ActiveGame } from "../game/ActiveGame";
import { GameResults } from "../game/GameResults";

// ── Helpers ──────────────────────────────────────────────────────────────────

const defaultState: GameState = {
  sessionId: "sess-1",
  sessionName: "Test Session",
  sessionStatus: "Active",
  elapsedSeconds: 30,
  clues: [],
  score: 0,
  connectionState: "Connected",
  ranking: [],
  currentQuestion: null,
  selectedAnswerIndex: null,
};

function withGame(ui: React.ReactElement, stateOverride: Partial<GameState> = {}) {
  const dispatch = vi.fn();
  return render(
    <MemoryRouter>
      <GameContext.Provider value={{ state: { ...defaultState, ...stateOverride }, dispatch }}>
        {ui}
      </GameContext.Provider>
    </MemoryRouter>
  );
}

function withRouter(ui: React.ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

beforeEach(() => vi.clearAllMocks());

// ── UnirseSesion ─────────────────────────────────────────────────────────────
describe("UnirseSesion", () => {
  it("renders PIN input and join button", () => {
    withRouter(<UnirseSesion />);
    expect(screen.getByLabelText(/pin/i)).toBeInTheDocument();
    // Button text is "Entrar a la Sesión"
    expect(screen.getByRole("button", { name: /entrar/i })).toBeInTheDocument();
  });

  it("shows validation error when PIN is too short", async () => {
    withRouter(<UnirseSesion />);
    fireEvent.change(screen.getByLabelText(/pin/i), { target: { value: "12" } });
    fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    await waitFor(() =>
      expect(screen.getByText(/6 caracteres/i)).toBeInTheDocument()
    );
  });

  it("calls joinSession when PIN is valid", async () => {
    vi.mocked(joinSession).mockResolvedValue({ sessionId: "s1", userId: "u1", joinedAt: "" });
    withRouter(<UnirseSesion />);
    fireEvent.change(screen.getByLabelText(/pin/i), { target: { value: "123456" } });
    fireEvent.click(screen.getByRole("button", { name: /entrar/i }));
    await waitFor(() => expect(joinSession).toHaveBeenCalledWith("123456"));
  });
});

// ── UnirseEquipo ─────────────────────────────────────────────────────────────
describe("UnirseEquipo", () => {
  it("renders join code input and button", () => {
    withRouter(<UnirseEquipo />);
    // Input has placeholder "ABC123" - no label, use placeholder
    expect(screen.getByPlaceholderText("ABC123")).toBeInTheDocument();
    // Button text is "Unirse al Equipo"
    expect(screen.getByRole("button", { name: /unirse al equipo/i })).toBeInTheDocument();
  });

  it("shows validation error when code is empty", async () => {
    withRouter(<UnirseEquipo />);
    // Submit without entering a code to trigger empty-string validation
    fireEvent.click(screen.getByRole("button", { name: /unirse al equipo/i }));
    await waitFor(() =>
      expect(screen.getByText(/obligatorio/i)).toBeInTheDocument()
    );
  });

  it("calls joinTeam when code is valid", async () => {
    vi.mocked(joinTeam).mockResolvedValue(undefined);
    withRouter(<UnirseEquipo />);
    fireEvent.change(screen.getByPlaceholderText("ABC123"), { target: { value: "ABC123" } });
    fireEvent.click(screen.getByRole("button", { name: /unirse al equipo/i }));
    await waitFor(() => expect(joinTeam).toHaveBeenCalled());
  });
});

// ── MiPerfil ─────────────────────────────────────────────────────────────────
describe("MiPerfil", () => {
  it("shows loading spinner initially", () => {
    vi.mocked(getProfile).mockReturnValue(new Promise(() => {}));
    withRouter(<MiPerfil />);
    // Loading state renders a spinner div, not text
    expect(document.querySelector(".spinner")).not.toBeNull();
  });

  it("shows profile form after load", async () => {
    vi.mocked(getProfile).mockResolvedValue({ name: "John Doe", alias: "jdoe", email: "john@test.com" });
    withRouter(<MiPerfil />);
    await waitFor(() => expect(screen.getByDisplayValue("John Doe")).toBeInTheDocument());
  });
});

// ── NextStageRedirect ────────────────────────────────────────────────────────
describe("NextStageRedirect", () => {
  it("renders without crashing", () => {
    withGame(<NextStageRedirect />);
    // Component navigates immediately; just verify no crash
    expect(document.body).toBeTruthy();
  });
});

// ── WaitingRoom ───────────────────────────────────────────────────────────────
describe("WaitingRoom", () => {
  it("shows session name and waiting message", () => {
    withGame(<WaitingRoom />, { sessionName: "Mi Sesión Especial" });
    expect(screen.getByText("Mi Sesión Especial")).toBeInTheDocument();
    expect(screen.getByText(/esperando/i)).toBeInTheDocument();
  });

  it("shows default text when no session name", () => {
    withGame(<WaitingRoom />, { sessionName: "" });
    expect(screen.getByText(/sesión de juego/i)).toBeInTheDocument();
  });
});

// ── ActiveGame ───────────────────────────────────────────────────────────────
describe("ActiveGame", () => {
  it("renders game components", () => {
    withGame(<ActiveGame />);
    expect(screen.getByText("Timer")).toBeInTheDocument();
    expect(screen.getByText("Ranking")).toBeInTheDocument();
  });

  it("shows waiting message when no question or clues", () => {
    withGame(<ActiveGame />, { currentQuestion: null, clues: [] });
    expect(screen.getByText(/esperando/i)).toBeInTheDocument();
  });
});

// ── GameResults ───────────────────────────────────────────────────────────────
describe("GameResults", () => {
  it("renders results page with ranking", () => {
    render(
      <MemoryRouter initialEntries={["/game/sess-1/results"]}>
        <GameContext.Provider value={{ state: { ...defaultState, sessionStatus: "Finished" }, dispatch: vi.fn() }}>
          <Routes>
            <Route path="/game/:sessionId/results" element={<GameResults />} />
          </Routes>
        </GameContext.Provider>
      </MemoryRouter>
    );
    expect(screen.getByText("Ranking")).toBeInTheDocument();
  });
});
