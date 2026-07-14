import { describe, it, expect, vi, beforeEach } from "vitest";
import { ApiError } from "../../services/teamsApi";
import type { TeamResponse, GetTeamsResponse } from "../../types/team";

// Mock the fetchWithAuth module
vi.mock("../../services/api", () => ({
  fetchWithAuth: vi.fn(),
}));

import { fetchWithAuth } from "../../services/api";
import {
  createTeam,
  listTeams,
  getTeamById,
  joinTeam,
} from "../../services/teamsApi";

const mockFetchWithAuth = vi.mocked(fetchWithAuth);

describe("ApiError", () => {
  it("creates error with status and body", () => {
    const error = new ApiError(404, "Not found");
    expect(error.status).toBe(404);
    expect(error.body).toBe("Not found");
    expect(error.name).toBe("ApiError");
    expect(error).toBeInstanceOf(Error);
  });
});

describe("teamsApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("createTeam returns team on success", async () => {
    const mockTeam: TeamResponse = {
      id: "team-1",
      name: "Test Team",
      description: "desc",
      leaderId: "l1",
      joinCode: "ABC123",
      createdAt: "2026-01-01",
    };

    mockFetchWithAuth.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockTeam),
    } as Response);

    const result = await createTeam({
      name: "Test Team",
      description: "desc",
      leaderId: "l1",
    });

    expect(result.name).toBe("Test Team");
    expect(result.joinCode).toBe("ABC123");
    expect(mockFetchWithAuth).toHaveBeenCalledWith(
      "/api/teams",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("createTeam throws ApiError on failure", async () => {
    mockFetchWithAuth.mockResolvedValue({
      ok: false,
      status: 400,
      text: () => Promise.resolve("Bad request"),
    } as Response);

    await expect(
      createTeam({ name: "Bad", description: "x", leaderId: "l1" })
    ).rejects.toThrow(ApiError);
  });

  it("listTeams returns paginated result", async () => {
    const mockResponse: GetTeamsResponse = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
    };

    mockFetchWithAuth.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockResponse),
    } as Response);

    const result = await listTeams({ page: 1, pageSize: 10 });
    expect(result.totalCount).toBe(0);
    expect(mockFetchWithAuth).toHaveBeenCalledWith(
      expect.stringContaining("/api/teams?"),
      expect.objectContaining({ method: "GET" })
    );
  });

  it("getTeamById returns team detail", async () => {
    const mockDetail = { id: "t1", name: "Team", description: "d", leaderId: "l1", members: [], stages: [] };

    mockFetchWithAuth.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockDetail),
    } as Response);

    const result = await getTeamById("t1");
    expect(result.name).toBe("Team");
  });

  it("joinTeam returns success on 204", async () => {
    mockFetchWithAuth.mockResolvedValue({
      ok: true,
      status: 204,
    } as Response);

    const result = await joinTeam("ABC123");
    expect(result.success).toBe(true);
  });
});
