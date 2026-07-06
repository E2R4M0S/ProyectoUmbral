import { describe, it, expect, vi, beforeEach } from "vitest";
import { ApiError } from "../../services/perfilApi";

vi.mock("../../services/api", () => ({
  fetchWithAuth: vi.fn(),
}));

import { fetchWithAuth } from "../../services/api";
import { getProfile, updateProfile } from "../../services/perfilApi";

const mockFetch = vi.mocked(fetchWithAuth);

describe("ApiError (perfilApi)", () => {
  it("is an Error with status and body", () => {
    const err = new ApiError(401, "Unauthorized");
    expect(err.status).toBe(401);
    expect(err.body).toBe("Unauthorized");
    expect(err).toBeInstanceOf(Error);
  });
});

describe("perfilApi", () => {
  beforeEach(() => vi.clearAllMocks());

  it("getProfile fetches /api/teams/profile", async () => {
    const mock = { name: "John", alias: "jdoe", email: "john@test.com" };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await getProfile();

    expect(result.alias).toBe("jdoe");
    expect(mockFetch).toHaveBeenCalledWith("/api/teams/profile");
  });

  it("getProfile throws ApiError on failure", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 401, text: () => Promise.resolve("Unauthorized") } as Response);
    await expect(getProfile()).rejects.toThrow(ApiError);
  });

  it("updateProfile puts to /api/teams/profile", async () => {
    const mock = { name: "Jane", alias: "jdoe2", email: "jane@test.com" };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await updateProfile({ name: "Jane", alias: "jdoe2" });

    expect(result.name).toBe("Jane");
    expect(mockFetch).toHaveBeenCalledWith("/api/teams/profile", expect.objectContaining({ method: "PUT" }));
  });

  it("updateProfile throws ApiError on failure", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 400, text: () => Promise.resolve("Bad") } as Response);
    await expect(updateProfile({ name: "", alias: "" })).rejects.toThrow(ApiError);
  });
});
