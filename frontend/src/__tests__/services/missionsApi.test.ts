import { describe, it, expect, vi, beforeEach } from "vitest";
import { ApiError } from "../../services/missionsApi";

vi.mock("../../services/api", () => ({
  fetchWithAuth: vi.fn(),
}));

import { fetchWithAuth } from "../../services/api";
import {
  createMission,
  changeMissionStatus,
  createStage,
  updateStage,
  createClue,
  deleteClue,
  deleteStage,
  listMissions,
  getMissionById,
  updateMission,
  getActiveMissions,
} from "../../services/missionsApi";

const mockFetch = vi.mocked(fetchWithAuth);

describe("ApiError (missionsApi)", () => {
  it("creates error with status and body", () => {
    const err = new ApiError(500, "Server error");
    expect(err.status).toBe(500);
    expect(err.body).toBe("Server error");
    expect(err.name).toBe("ApiError");
  });
});

describe("missionsApi", () => {
  beforeEach(() => vi.clearAllMocks());

  it("createMission posts to /api/admin/missions", async () => {
    const mock = { id: "m1", title: "Test", status: "Draft" };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await createMission({ title: "Test", description: "d", difficulty: "Easy", timeMinutes: 30, type: "Treasure" });

    expect(result.id).toBe("m1");
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/missions", expect.objectContaining({ method: "POST" }));
  });

  it("createMission throws ApiError on failure", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 400, text: () => Promise.resolve("Bad") } as Response);
    await expect(createMission({ title: "", description: "", difficulty: "Easy", timeMinutes: 30, type: "Treasure" })).rejects.toThrow(ApiError);
  });

  it("changeMissionStatus patches /api/admin/missions/{id}/status", async () => {
    mockFetch.mockResolvedValue({ ok: true } as Response);
    await changeMissionStatus("m1", "Active");
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/missions/m1/status", expect.objectContaining({ method: "PATCH" }));
  });

  it("changeMissionStatus throws ApiError on failure", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 400, text: () => Promise.resolve("Bad") } as Response);
    await expect(changeMissionStatus("m1", "Active")).rejects.toThrow(ApiError);
  });

  it("createStage posts to /api/admin/missions/{id}/stages", async () => {
    mockFetch.mockResolvedValue({ ok: true } as Response);
    await createStage("m1", { name: "Stage 1", description: "desc", order: 1 });
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/missions/m1/stages", expect.objectContaining({ method: "POST" }));
  });

  it("updateStage puts to /api/admin/missions/{id}/stages/{stageId}", async () => {
    mockFetch.mockResolvedValue({ ok: true } as Response);
    await updateStage("m1", "s1", { name: "Updated", description: "d", order: 1 });
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/missions/m1/stages/s1", expect.objectContaining({ method: "PUT" }));
  });

  it("createClue posts to /api/admin/missions/{id}/stages/{stageId}/clues", async () => {
    mockFetch.mockResolvedValue({ ok: true } as Response);
    await createClue("m1", "s1", { content: "Look under the bridge", penalty: 5 });
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/missions/m1/stages/s1/clues", expect.objectContaining({ method: "POST" }));
  });

  it("deleteClue sends DELETE to /api/admin/missions/{id}/stages/{stageId}/clues/{clueId}", async () => {
    mockFetch.mockResolvedValue({ ok: true } as Response);
    await deleteClue("m1", "s1", "c1");
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/missions/m1/stages/s1/clues/c1", expect.objectContaining({ method: "DELETE" }));
  });

  it("deleteStage sends DELETE to /api/admin/missions/{id}/stages/{stageId}", async () => {
    mockFetch.mockResolvedValue({ ok: true } as Response);
    await deleteStage("m1", "s1");
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/missions/m1/stages/s1", expect.objectContaining({ method: "DELETE" }));
  });

  it("listMissions returns paginated missions", async () => {
    const mock = { items: [], totalCount: 0, page: 1, pageSize: 10 };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await listMissions({ page: 1, pageSize: 10 });

    expect(result.totalCount).toBe(0);
    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining("/api/missions?"), expect.objectContaining({ method: "GET" }));
  });

  it("listMissions includes difficulty filter when provided", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ items: [], totalCount: 0, page: 1, pageSize: 10 }) } as Response);
    await listMissions({ page: 1, pageSize: 10, difficulty: "Hard" });
    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining("difficulty=Hard"), expect.anything());
  });

  it("getMissionById fetches /api/missions/{id}", async () => {
    const mock = { id: "m1", title: "Test", stages: [] };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await getMissionById("m1");

    expect(result.id).toBe("m1");
    expect(mockFetch).toHaveBeenCalledWith("/api/missions/m1", expect.objectContaining({ method: "GET" }));
  });

  it("getMissionById throws ApiError on 404", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 404, text: () => Promise.resolve("Not found") } as Response);
    await expect(getMissionById("bad")).rejects.toThrow(ApiError);
  });

  it("updateMission puts to /api/admin/missions/{id}", async () => {
    const mock = { id: "m1", title: "Updated", status: "Active" };
    mockFetch.mockResolvedValue({ ok: true, status: 200, json: () => Promise.resolve(mock) } as Response);

    const result = await updateMission("m1", { title: "Updated", description: "d", difficulty: "Hard", timeMinutes: 60, type: "Treasure" });

    expect(result.title).toBe("Updated");
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/missions/m1", expect.objectContaining({ method: "PUT" }));
  });

  it("updateMission returns minimal object on 204", async () => {
    mockFetch.mockResolvedValue({ ok: true, status: 204 } as Response);
    const result = await updateMission("m1", { title: "T", description: "d", difficulty: "Easy", timeMinutes: 30, type: "Trivia" });
    expect(result.id).toBe("m1");
  });

  it("getActiveMissions fetches /api/missions/active", async () => {
    const mock = { items: [], totalCount: 0, page: 1, pageSize: 10 };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await getActiveMissions({ page: 1, pageSize: 10 });

    expect(result.totalCount).toBe(0);
    expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining("/api/missions/active?"), expect.objectContaining({ method: "GET" }));
  });
});
