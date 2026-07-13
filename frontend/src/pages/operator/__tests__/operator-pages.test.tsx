import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";

vi.mock("../../../services/sessionsApi", () => ({
  startSession: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

// HubConnectionBuilder must be mocked as a class (called with "new")
vi.mock("@microsoft/signalr", () => {
  class MockHubConnectionBuilder {
    withUrl()                { return this; }
    withAutomaticReconnect() { return this; }
    build() {
      return {
        on:    vi.fn(),
        off:   vi.fn(),
        start: vi.fn().mockResolvedValue(undefined),
        stop:  vi.fn().mockResolvedValue(undefined),
      };
    }
  }
  return { HubConnectionBuilder: MockHubConnectionBuilder };
});

import { startSession } from "../../../services/sessionsApi";
import { IniciarJuego } from "../IniciarJuego";
import QuestionResults from "../QuestionResults";

beforeEach(() => vi.clearAllMocks());

function wrap(ui: React.ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

// ── IniciarJuego ──────────────────────────────────────────────────────────────
describe("IniciarJuego", () => {
  it("renders session name and start button", () => {
    wrap(<IniciarJuego sessionId="s1" sessionName="Gran Sesión" />);
    expect(screen.getByText(/Gran Sesión/i)).toBeInTheDocument();
    // Button text is "▶ Iniciar Partida"
    expect(screen.getByRole("button", { name: /iniciar partida/i })).toBeInTheDocument();
  });

  it("calls startSession when form is submitted", async () => {
    vi.mocked(startSession).mockResolvedValue({ id: "s1", status: "Active" });
    wrap(<IniciarJuego sessionId="s1" sessionName="Test" />);
    fireEvent.click(screen.getByRole("button", { name: /iniciar partida/i }));
    await waitFor(() => expect(startSession).toHaveBeenCalledWith("s1"));
  });

  it("calls onGameStarted callback after success", async () => {
    vi.mocked(startSession).mockResolvedValue({ id: "s1", status: "Active" });
    const onStarted = vi.fn();
    wrap(<IniciarJuego sessionId="s1" sessionName="Test" onGameStarted={onStarted} />);
    fireEvent.click(screen.getByRole("button", { name: /iniciar partida/i }));
    await waitFor(() => expect(onStarted).toHaveBeenCalled());
  });

  it("shows connection error when start throws non-ApiError", async () => {
    // Non-ApiError → "Error de conexión. Verificá tu conexión a internet."
    vi.mocked(startSession).mockRejectedValue(new Error("Network failure"));
    wrap(<IniciarJuego sessionId="bad" sessionName="Test" />);
    fireEvent.click(screen.getByRole("button", { name: /iniciar partida/i }));
    await waitFor(() =>
      expect(screen.getByText(/error de conexión/i)).toBeInTheDocument()
    );
  });
});

// ── QuestionResults ───────────────────────────────────────────────────────────
describe("QuestionResults", () => {
  it("renders without crashing", () => {
    wrap(<QuestionResults quizId="q1" />);
    expect(document.body).toBeTruthy();
  });
});
