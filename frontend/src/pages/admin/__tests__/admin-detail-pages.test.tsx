import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";

const FAKE_TOKEN = "header.eyJyZWFsbV9hY2Nlc3MiOnsicm9sZXMiOlsiYWRtaW4iXX19.sig";
vi.mock("react-oidc-context", () => ({
  useAuth: () => ({ user: { access_token: FAKE_TOKEN } }),
}));

vi.mock("../../../services/missionsApi", () => ({
  getMissionById: vi.fn(),
  createStage: vi.fn(),
  updateStage: vi.fn(),
  deleteStage: vi.fn(),
  createClue: vi.fn(),
  deleteClue: vi.fn(),
  updateMission: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/sessionsApi", () => ({
  getSessionProgress: vi.fn(),
  getSessionById: vi.fn(),
  transitionSession: vi.fn(),
  advanceStage: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/teamsApi", () => ({
  getTeamById: vi.fn(),
  updateTeam: vi.fn(),
  removeMember: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/adminUsuariosApi", () => ({
  getUserById: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

vi.mock("../../../services/api", () => ({
  fetchWithAuth: vi.fn(),
}));

import { getMissionById } from "../../../services/missionsApi";
import { getSessionProgress, getSessionById } from "../../../services/sessionsApi";
import { getTeamById } from "../../../services/teamsApi";
import { getUserById } from "../../../services/adminUsuariosApi";
import { fetchWithAuth } from "../../../services/api";

import { DetalleMision } from "../DetalleMision";
import { DetalleUsuario } from "../DetalleUsuario";
import { EditarMision } from "../EditarMision";
import { EditarEquipo } from "../EditarEquipo";
import { EquipoDetalle } from "../EquipoDetalle";
import { PanelSesion } from "../PanelSesion";

beforeEach(() => vi.clearAllMocks());

function renderWithId(component: React.ReactElement, path: string, route: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path={route} element={component} />
      </Routes>
    </MemoryRouter>
  );
}

// ── DetalleMision ────────────────────────────────────────────────────────────
describe("DetalleMision", () => {
  it("shows loading state initially", () => {
    vi.mocked(getMissionById).mockReturnValue(new Promise(() => {}));
    renderWithId(<DetalleMision />, "/admin/misiones/m1", "/admin/misiones/:id");
    expect(screen.getByText(/cargando/i)).toBeInTheDocument();
  });

  it("shows mission data after load", async () => {
    vi.mocked(getMissionById).mockResolvedValue({
      id: "m1", title: "Misión Test", description: "Desc", difficulty: "Easy",
      type: "Treasure", status: "Draft", timeMinutes: 30, stages: []
    });
    renderWithId(<DetalleMision />, "/admin/misiones/m1", "/admin/misiones/:id");
    await waitFor(() => expect(screen.getByText("Misión Test")).toBeInTheDocument());
  });
});

// ── DetalleUsuario ────────────────────────────────────────────────────────────
describe("DetalleUsuario", () => {
  it("shows loading spinner initially", () => {
    vi.mocked(getUserById).mockReturnValue(new Promise(() => {}));
    renderWithId(<DetalleUsuario />, "/admin/usuarios/u1", "/admin/usuarios/:id");
    expect(document.querySelector(".spinner")).not.toBeNull();
  });

  it("shows user data after load", async () => {
    vi.mocked(getUserById).mockResolvedValue({
      id: "u1", name: "Alice Doe", email: "alice@test.com",
      roles: ["operator"], emailVerified: true,
      enabled: true, attributes: {}, createdAt: ""
    });
    renderWithId(<DetalleUsuario />, "/admin/usuarios/u1", "/admin/usuarios/:id");
    await waitFor(() => expect(screen.getByText("Alice Doe")).toBeInTheDocument());
  });
});

// ── EditarMision ─────────────────────────────────────────────────────────────
describe("EditarMision", () => {
  it("shows loading initially", () => {
    vi.mocked(getMissionById).mockReturnValue(new Promise(() => {}));
    renderWithId(<EditarMision />, "/admin/misiones/m1/editar", "/admin/misiones/:id/editar");
    expect(screen.getByText(/cargando/i)).toBeInTheDocument();
  });

  it("shows edit form after load", async () => {
    vi.mocked(getMissionById).mockResolvedValue({
      id: "m1", title: "Misión Editable", description: "Desc", difficulty: "Medium",
      type: "Treasure", status: "Draft", timeMinutes: 60, stages: []
    });
    renderWithId(<EditarMision />, "/admin/misiones/m1/editar", "/admin/misiones/:id/editar");
    await waitFor(() => expect(screen.getByDisplayValue("Misión Editable")).toBeInTheDocument());
  });
});

// ── EditarEquipo ─────────────────────────────────────────────────────────────
describe("EditarEquipo", () => {
  it("shows loading initially", () => {
    vi.mocked(getTeamById).mockReturnValue(new Promise(() => {}));
    renderWithId(<EditarEquipo />, "/admin/equipos/t1/editar", "/admin/equipos/:id/editar");
    expect(screen.getByText(/cargando/i)).toBeInTheDocument();
  });

  it("shows edit form after load", async () => {
    vi.mocked(getTeamById).mockResolvedValue({
      id: "t1", name: "Team Beta", description: "Test team",
      leaderId: "u1", leaderName: "", joinCode: "XYZ123", members: [], createdAt: ""
    });
    renderWithId(<EditarEquipo />, "/admin/equipos/t1/editar", "/admin/equipos/:id/editar");
    await waitFor(() => expect(screen.getByDisplayValue("Team Beta")).toBeInTheDocument());
  });
});

// ── EquipoDetalle ─────────────────────────────────────────────────────────────
describe("EquipoDetalle", () => {
  it("shows loading initially", () => {
    vi.mocked(getTeamById).mockReturnValue(new Promise(() => {}));
    renderWithId(<EquipoDetalle />, "/admin/equipos/t1", "/admin/equipos/:id");
    expect(screen.getByText(/cargando/i)).toBeInTheDocument();
  });

  it("shows team data after load", async () => {
    vi.mocked(getTeamById).mockResolvedValue({
      id: "t1", name: "Team Gamma", description: "Gamma desc",
      leaderId: "u1", leaderName: "", joinCode: "AAA111", members: [], createdAt: ""
    });
    renderWithId(<EquipoDetalle />, "/admin/equipos/t1", "/admin/equipos/:id");
    await waitFor(() => expect(screen.getByText("Team Gamma")).toBeInTheDocument());
  });
});

// ── PanelSesion ──────────────────────────────────────────────────────────────
describe("PanelSesion", () => {
  it("shows loading initially", () => {
    vi.mocked(getSessionProgress).mockReturnValue(new Promise(() => {}));
    vi.mocked(getSessionById).mockReturnValue(new Promise(() => {}));
    vi.mocked(fetchWithAuth).mockReturnValue(new Promise(() => {}));
    renderWithId(<PanelSesion />, "/admin/sesiones/s1", "/admin/sesiones/:id");
    expect(screen.getByText(/cargando/i)).toBeInTheDocument();
  });

  it("shows session panel after load", async () => {
    vi.mocked(getSessionProgress).mockResolvedValue({
      sessionId: "s1", name: "Sesión Live", status: "Active",
      elapsedSeconds: 120, participants: [], teamId: null, teamName: null
    });
    vi.mocked(getSessionById).mockResolvedValue({
      id: "s1", name: "Sesión Live", pin: "111222", status: "Active",
      stages: [{ missionId: "m1", missionTitle: "M1", stageName: "M1", missionType: "Treasure", order: 1 }],
      currentStageOrder: 0, teamId: null, teamName: null, participants: [], startedAt: null, finishedAt: null, createdAt: ""
    });
    vi.mocked(fetchWithAuth).mockResolvedValue({ ok: true, json: () => Promise.resolve([]) } as Response);
    renderWithId(<PanelSesion />, "/admin/sesiones/s1", "/admin/sesiones/:id");
    await waitFor(() => expect(screen.getByText("Sesión Live")).toBeInTheDocument());
  });
});
