import { describe, it, expect, vi, beforeEach } from "vitest";
import { ApiError } from "../../services/adminUsuariosApi";

vi.mock("../../services/api", () => ({
  fetchWithAuth: vi.fn(),
}));

import { fetchWithAuth } from "../../services/api";
import { getUsers, getUserById } from "../../services/adminUsuariosApi";

const mockFetch = vi.mocked(fetchWithAuth);

describe("ApiError (adminUsuariosApi)", () => {
  it("creates error with status and body", () => {
    const err = new ApiError(500, "Server error");
    expect(err.status).toBe(500);
    expect(err.body).toBe("Server error");
    expect(err).toBeInstanceOf(Error);
  });
});

describe("getUsers", () => {
  beforeEach(() => vi.clearAllMocks());

  it("fetches /api/admin/users with no params", async () => {
    const mock = { users: [], total: 0 };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await getUsers();

    expect(mockFetch).toHaveBeenCalledWith("/api/admin/users");
    expect(result).toEqual(mock);
  });

  it("builds query string from params", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ users: [], total: 0 }) } as Response);

    await getUsers({ search: "alice", role: "operator", enabled: true, page: 2, pageSize: 5 });

    const url = mockFetch.mock.calls[0][0] as string;
    expect(url).toContain("search=alice");
    expect(url).toContain("role=operator");
    expect(url).toContain("enabled=true");
    expect(url).toContain("page=2");
    expect(url).toContain("pageSize=5");
  });

  it("throws ApiError on failure", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 403, text: () => Promise.resolve("Forbidden") } as Response);
    await expect(getUsers()).rejects.toThrow(ApiError);
  });
});

describe("getUserById", () => {
  beforeEach(() => vi.clearAllMocks());

  it("fetches /api/admin/users/{id}", async () => {
    const mock = { id: "u1", name: "Alice", email: "alice@test.com", role: "operator" };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await getUserById("u1");

    expect(result.id).toBe("u1");
    expect(mockFetch).toHaveBeenCalledWith("/api/admin/users/u1");
  });

  it("URL-encodes the user id", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ id: "a b", name: "", email: "", role: "" }) } as Response);

    await getUserById("a b");

    expect(mockFetch).toHaveBeenCalledWith("/api/admin/users/a%20b");
  });

  it("throws ApiError on 404", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 404, text: () => Promise.resolve("Not found") } as Response);
    await expect(getUserById("bad")).rejects.toThrow(ApiError);
  });
});
