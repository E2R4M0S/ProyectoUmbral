import { describe, it, expect, vi, beforeEach } from "vitest";
import { ApiError } from "../../services/sessionsApi";

vi.mock("../../services/api", () => ({
  fetchWithAuth: vi.fn(),
}));

import { fetchWithAuth } from "../../services/api";
import {
  joinSession,
  startSession,
  transitionSession,
  finishSession,
  createSession,
  advanceStage,
  listSessions,
  getSessionById,
  getSessionProgress,
} from "../../services/sessionsApi";

const mockFetch = vi.mocked(fetchWithAuth);

describe("ApiError (sessionsApi)", () => {
  it("creates error with status and body", () => {
    const err = new ApiError(400, "Bad");
    expect(err.status).toBe(400);
    expect(err.body).toBe("Bad");
    expect(err.name).toBe("ApiError");
    expect(err).toBeInstanceOf(Error);
  });
});

describe("sessionsApi", () => {
  beforeEach(() => vi.clearAllMocks());

  it("joinSession posts to /api/sessions/join and returns result", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ sessionId: "s1", userId: "u1", joinedAt: "" }) } as Response);

    const result = await joinSession("123456");

    expect(result.sessionId).toBe("s1");
    expect(mockFetch).toHaveBeenCalledWith("/api/sessions/join", expect.objectContaining({ method: "POST" }));
  });

  it("joinSession throws ApiError on failure", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 400, text: () => Promise.resolve("Bad") } as Response);
    await expect(joinSession("bad")).rejects.toThrow(ApiError);
  });

  it("startSession posts to /api/sessions/{id}/start", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ id: "s1", status: "Active" }) } as Response);

    const result = await startSession("s1");

    expect(result.status).toBe("Active");
    expect(mockFetch).toHaveBeenCalledWith("/api/sessions/s1/start", expect.objectContaining({ method: "POST" }));
  });

  it("transitionSession patches /api/sessions/{id}/status", async () => {
    mockFetch.mockResolvedValue({ ok: true } as Response);
    await transitionSession("s1", "Active");
    expect(mockFetch).toHaveBeenCalledWith("/api/sessions/s1/status", expect.objectContaining({ method: "PATCH" }));
  });

  it("transitionSession throws ApiError on failure", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 400, text: () => Promise.resolve("bad") } as Response);
    await expect(transitionSession("s1", "Bad")).rejects.toThrow(ApiError);
  });

  it("finishSession posts to /api/sessions/{id}/finish", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ id: "s1", status: "Finished" }) } as Response);

    const result = await finishSession("s1");

    expect(result.status).toBe("Finished");
    expect(mockFetch).toHaveBeenCalledWith("/api/sessions/s1/finish", expect.objectContaining({ method: "POST" }));
  });

  it("createSession posts to /api/sessions", async () => {
    const mockResponse = { id: "s1", name: "Test", pin: "123456", status: "Scheduled" };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mockResponse) } as Response);

    const result = await createSession({ name: "Test", stages: [] });

    expect(result.pin).toBe("123456");
    expect(mockFetch).toHaveBeenCalledWith("/api/sessions", expect.objectContaining({ method: "POST" }));
  });

  it("advanceStage patches /api/sessions/{id}/advance-stage", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ currentStageOrder: 1, totalStages: 2, isLastStage: false }) } as Response);

    const result = await advanceStage("s1");

    expect(result.currentStageOrder).toBe(1);
    expect(mockFetch).toHaveBeenCalledWith("/api/sessions/s1/advance-stage", expect.objectContaining({ method: "PATCH" }));
  });

  it("listSessions returns paginated results", async () => {
    const mockResult = { items: [], totalCount: 0, page: 1, pageSize: 10 };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mockResult) } as Response);

    const result = await listSessions({ page: 1, pageSize: 10 });

    expect(result.totalCount).toBe(0);
    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining("/api/sessions?"), expect.objectContaining({ method: "GET" }));
  });

  it("listSessions includes status filter when provided", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ items: [], totalCount: 0, page: 1, pageSize: 10 }) } as Response);

    await listSessions({ page: 1, pageSize: 10, status: "Active" });

    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining("status=Active"), expect.anything());
  });

  it("getSessionById fetches /api/sessions/{id}", async () => {
    const mockDetail = { id: "s1", name: "Test", pin: "123456", status: "Active" };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mockDetail) } as Response);

    const result = await getSessionById("s1");

    expect(result.name).toBe("Test");
    expect(mockFetch).toHaveBeenCalledWith("/api/sessions/s1", expect.objectContaining({ method: "GET" }));
  });

  it("getSessionById throws ApiError on 404", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 404, text: () => Promise.resolve("Not found") } as Response);
    await expect(getSessionById("bad")).rejects.toThrow(ApiError);
  });

  it("getSessionProgress fetches /api/sessions/{id}/progress", async () => {
    const mockProgress = { sessionId: "s1", name: "Test", status: "Active", elapsedSeconds: 120, participants: [] };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mockProgress) } as Response);

    const result = await getSessionProgress("s1");

    expect(result.elapsedSeconds).toBe(120);
    expect(mockFetch).toHaveBeenCalledWith("/api/sessions/s1/progress", expect.objectContaining({ method: "GET" }));
  });
});
