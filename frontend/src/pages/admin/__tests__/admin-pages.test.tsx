import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";

// access_token payload: {"realm_access":{"roles":["admin"]}}
const FAKE_TOKEN = "header.eyJyZWFsbV9hY2Nlc3MiOnsicm9sZXMiOlsiYWRtaW4iXX19.sig";

vi.mock("react-oidc-context", () => ({
  useAuth: () => ({ user: { access_token: FAKE_TOKEN } }),
}));

vi.mock("../../../services/missionsApi", () => ({
  listMissions: vi.fn(),
  changeMissionStatus: vi.fn(),
  createMission: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/sessionsApi", () => ({
  listSessions: vi.fn(),
  createSession: vi.fn(),
  transitionSession: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/teamsApi", () => ({
  listTeams: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/operadorApi", () => ({
  crearOperador: vi.fn(),
  desactivarOperador: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/adminUsuariosApi", () => ({
  getUsers: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/api", () => ({
  fetchWithAuth: vi.fn(),
}));

vi.mock("../../../services/triviaApi", () => ({
  listQuizzes: vi.fn().mockResolvedValue([]),
}));

import { listMissions } from "../../../services/missionsApi";
import { listSessions } from "../../../services/sessionsApi";
import { listTeams } from "../../../services/teamsApi";
import { getUsers } from "../../../services/adminUsuariosApi";
import { fetchWithAuth } from "../../../services/api";

import { CatalogoMisiones } from "../CatalogoMisiones";
import { CrearMision } from "../CrearMision";
import { CrearOperador } from "../CrearOperador";
import { CrearSesion } from "../CrearSesion";
import { DesactivarOperador } from "../DesactivarOperador";
import { ListadoEquipos } from "../ListadoEquipos";
import { ListadoSesiones } from "../ListadoSesiones";
import { ListadoUsuarios } from "../ListadoUsuarios";
import { QuizBank } from "../QuizBank";

const wrap = (ui: React.ReactElement) =>
  render(<MemoryRouter initialEntries={["/admin"]}>{ui}</MemoryRouter>);

beforeEach(() => vi.clearAllMocks());

// ── CatalogoMisiones ────────────────────────────────────────────────────────
describe("CatalogoMisiones", () => {
  it("renders and calls listMissions on mount", async () => {
    // Component uses result.items, not result.missions
    vi.mocked(listMissions).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 10 } as any);
    wrap(<CatalogoMisiones />);
    await waitFor(() => expect(listMissions).toHaveBeenCalled());
    expect(screen.getByRole("heading", { name: /catálogo de misiones/i })).toBeInTheDocument();
  });

  it("shows missions when loaded", async () => {
    const item = { id: "m1", title: "Misión Alpha", difficulty: "Easy", type: "Treasure", status: "Active", timeMinutes: 30, stageCount: 2, createdAt: "" };
    vi.mocked(listMissions).mockResolvedValue({ items: [item], totalCount: 1, page: 1, pageSize: 10 } as any);
    wrap(<CatalogoMisiones />);
    await waitFor(() => expect(screen.getByText("Misión Alpha")).toBeInTheDocument());
  });
});

// ── CrearMision ─────────────────────────────────────────────────────────────
describe("CrearMision", () => {
  it("renders the mission creation form", () => {
    wrap(<CrearMision />);
    expect(screen.getByLabelText(/título/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /crear misión/i })).toBeInTheDocument();
  });

  it("shows validation errors on empty submit", async () => {
    wrap(<CrearMision />);
    fireEvent.click(screen.getByRole("button", { name: /crear misión/i }));
    await waitFor(() =>
      expect(screen.getByText(/título es obligatorio/i)).toBeInTheDocument()
    );
  });
});

// ── CrearOperador ───────────────────────────────────────────────────────────
describe("CrearOperador", () => {
  it("renders operator creation form with all fields", () => {
    wrap(<CrearOperador />);
    expect(screen.getByLabelText(/nombre/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/contraseña/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /crear operador/i })).toBeInTheDocument();
  });

  it("shows validation errors on empty submit", async () => {
    wrap(<CrearOperador />);
    fireEvent.click(screen.getByRole("button", { name: /crear operador/i }));
    await waitFor(() =>
      expect(screen.getByText(/nombre es obligatorio/i)).toBeInTheDocument()
    );
  });
});

// ── CrearSesion ─────────────────────────────────────────────────────────────
describe("CrearSesion", () => {
  it("renders session name field and submit button", () => {
    vi.mocked(listMissions).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 10 } as any);
    wrap(<CrearSesion />);
    expect(screen.getByLabelText(/nombre/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /crear sesión/i })).toBeInTheDocument();
  });

  it("shows name validation error on empty submit", async () => {
    vi.mocked(listMissions).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 10 } as any);
    wrap(<CrearSesion />);
    fireEvent.click(screen.getByRole("button", { name: /crear sesión/i }));
    await waitFor(() =>
      expect(screen.getByText(/nombre es obligatorio/i)).toBeInTheDocument()
    );
  });
});

// ── DesactivarOperador ──────────────────────────────────────────────────────
describe("DesactivarOperador", () => {
  it("renders the deactivation form", () => {
    wrap(<DesactivarOperador />);
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /desactivar/i })).toBeInTheDocument();
  });

  it("shows error when email is empty", async () => {
    wrap(<DesactivarOperador />);
    fireEvent.click(screen.getByRole("button", { name: /desactivar/i }));
    await waitFor(() =>
      expect(screen.getByText(/email es obligatorio/i)).toBeInTheDocument()
    );
  });
});

// ── ListadoEquipos ──────────────────────────────────────────────────────────
describe("ListadoEquipos", () => {
  it("renders and loads teams", async () => {
    // Component uses result.items (not result.teams)
    vi.mocked(listTeams).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 10 } as any);
    wrap(<ListadoEquipos />);
    await waitFor(() => expect(listTeams).toHaveBeenCalled());
    expect(screen.getByRole("heading", { name: /listado de equipos/i })).toBeInTheDocument();
  });
});

// ── ListadoSesiones ─────────────────────────────────────────────────────────
describe("ListadoSesiones", () => {
  it("renders and loads sessions", async () => {
    // Component uses r.items (not r.sessions)
    vi.mocked(listSessions).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 10 } as any);
    wrap(<ListadoSesiones />);
    await waitFor(() => expect(listSessions).toHaveBeenCalled());
    expect(screen.getByRole("heading")).toBeInTheDocument();
  });
});

// ── ListadoUsuarios ─────────────────────────────────────────────────────────
describe("ListadoUsuarios", () => {
  it("renders and loads users", async () => {
    // Component uses result.items (not result.users)
    vi.mocked(getUsers).mockResolvedValue({ items: [], total: 0 } as any);
    wrap(<ListadoUsuarios />);
    await waitFor(() => expect(getUsers).toHaveBeenCalled());
    expect(screen.getByRole("heading")).toBeInTheDocument();
  });
});

// ── QuizBank ────────────────────────────────────────────────────────────────
describe("QuizBank", () => {
  it("renders banco de preguntas heading", async () => {
    vi.mocked(fetchWithAuth).mockResolvedValue({ ok: true, json: () => Promise.resolve([]) } as Response);
    wrap(<QuizBank />);
    await waitFor(() => expect(fetchWithAuth).toHaveBeenCalled());
    expect(screen.getByText(/banco de preguntas/i)).toBeInTheDocument();
  });
});
