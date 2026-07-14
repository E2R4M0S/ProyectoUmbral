import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock("html5-qrcode", () => ({
  Html5Qrcode: class {
    start(_: unknown, __: unknown, onSuccess: (text: string) => void) {
      // Expose for tests to call manually
      (globalThis as Record<string, unknown>).__qrScanCallback__ = onSuccess;
      return Promise.resolve();
    }
    stop() { return Promise.resolve(); }
    clear() {}
  },
}));

vi.mock("../../../services/sessionsApi", () => ({
  validateQr: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

import { validateQr } from "../../../services/sessionsApi";
import { EscanearQr } from "../game/EscanearQr";
import { MisionCompletada } from "../game/MisionCompletada";

// ── Helpers ──────────────────────────────────────────────────────────────────

function renderEscanearQr(sessionId = "sess-1") {
  return render(
    <MemoryRouter initialEntries={[`/juego/${sessionId}/escanear`]}>
      <Routes>
        <Route path="/juego/:sessionId/escanear" element={<EscanearQr />} />
        <Route path="/juego/:sessionId/completada" element={<MisionCompletada />} />
      </Routes>
    </MemoryRouter>
  );
}

function triggerQrScan(text: string) {
  const cb = (globalThis as Record<string, unknown>).__qrScanCallback__ as ((t: string) => void) | undefined;
  cb?.(text);
}

beforeEach(() => {
  vi.clearAllMocks();
  delete (globalThis as Record<string, unknown>).__qrScanCallback__;
  sessionStorage.setItem("joined_sess-1", "1");
  // Force live-camera path so Html5Qrcode.start() fires and __qrScanCallback__ is set
  Object.defineProperty(window, "isSecureContext", { value: true, configurable: true });
});

// ── EscanearQr ────────────────────────────────────────────────────────────────

describe("EscanearQr", () => {
  it("renders scanner and prompt text", () => {
    renderEscanearQr();
    expect(screen.getByRole("heading", { name: /escanear código qr/i })).toBeInTheDocument();
    expect(screen.getByText(/apuntá la cámara/i)).toBeInTheDocument();
  });

  it("shows success state when QR is valid and stage advances", async () => {
    vi.mocked(validateQr).mockResolvedValue({
      isValid: true, advanced: true, currentStageOrder: 1,
      totalStages: 3, isLastStage: false, errorMessage: null,
      isAtGate: false, gateOpened: false, gatePosition: 0, gateThreshold: 0, isEliminated: false,
    });

    renderEscanearQr();
    triggerQrScan(JSON.stringify({ stageId: "stage-1", token: "tok1" }));

    await waitFor(() =>
      expect(screen.getByText(/etapa superada/i)).toBeInTheDocument()
    );
    expect(screen.getAllByText(/etapa 1 de 3/i).length).toBeGreaterThan(0);
  });

  it("shows error when QR payload is not valid JSON", async () => {
    renderEscanearQr();
    triggerQrScan("not-json-at-all");

    await waitFor(() =>
      expect(screen.getByText(/no es válido/i)).toBeInTheDocument()
    );
    expect(screen.getByRole("button", { name: /intentar de nuevo/i })).toBeInTheDocument();
  });

  it("shows error when server rejects the QR", async () => {
    vi.mocked(validateQr).mockResolvedValue({
      isValid: false, advanced: false, currentStageOrder: 0,
      totalStages: 2, isLastStage: false, errorMessage: "Invalid QR code for current stage",
      isAtGate: false, gateOpened: false, gatePosition: 0, gateThreshold: 0, isEliminated: false,
    });

    renderEscanearQr();
    triggerQrScan(JSON.stringify({ stageId: "wrong-id", token: "bad-token" }));

    await waitFor(() =>
      expect(screen.getByText(/invalid qr code for current stage/i)).toBeInTheDocument()
    );
  });

  it("retry button resets to scanning state", async () => {
    vi.mocked(validateQr).mockResolvedValue({
      isValid: false, advanced: false, currentStageOrder: 0,
      totalStages: 1, isLastStage: false, errorMessage: "Error",
      isAtGate: false, gateOpened: false, gatePosition: 0, gateThreshold: 0, isEliminated: false,
    });

    renderEscanearQr();
    triggerQrScan(JSON.stringify({ stageId: "s", token: "t" }));

    await waitFor(() => screen.getByRole("button", { name: /intentar de nuevo/i }));
    fireEvent.click(screen.getByRole("button", { name: /intentar de nuevo/i }));

    await waitFor(() =>
      expect(screen.getByText(/apuntá la cámara/i)).toBeInTheDocument()
    );
  });

  it("navigates to completada when last stage is scanned", async () => {
    vi.mocked(validateQr).mockResolvedValue({
      isValid: true, advanced: false, currentStageOrder: 2,
      totalStages: 2, isLastStage: true, errorMessage: null,
      isAtGate: false, gateOpened: false, gatePosition: 0, gateThreshold: 0, isEliminated: false,
    });

    renderEscanearQr();
    triggerQrScan(JSON.stringify({ stageId: "last-stage", token: "final-tok" }));

    await waitFor(() =>
      expect(screen.getByText(/última etapa completada/i)).toBeInTheDocument()
    );
  });
});

// ── MisionCompletada ─────────────────────────────────────────────────────────

describe("MisionCompletada", () => {
  it("renders completion message", () => {
    render(
      <MemoryRouter initialEntries={["/juego/sess-1/completada"]}>
        <Routes>
          <Route path="/juego/:sessionId/completada" element={<MisionCompletada />} />
        </Routes>
      </MemoryRouter>
    );
    expect(screen.getByRole("heading", { name: /misión completada/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /volver al inicio/i })).toBeInTheDocument();
  });

  it("return button navigates to home", () => {
    render(
      <MemoryRouter initialEntries={["/juego/sess-1/completada"]}>
        <Routes>
          <Route path="/" element={<div>Home</div>} />
          <Route path="/juego/:sessionId/completada" element={<MisionCompletada />} />
        </Routes>
      </MemoryRouter>
    );
    fireEvent.click(screen.getByRole("button", { name: /volver al inicio/i }));
    expect(screen.getByText("Home")).toBeInTheDocument();
  });
});
