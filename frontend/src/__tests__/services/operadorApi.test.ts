import { describe, it, expect, vi, beforeEach } from "vitest";
import { ApiError } from "../../services/operadorApi";

vi.mock("../../services/api", () => ({
  fetchWithAuth: vi.fn(),
}));

import { fetchWithAuth } from "../../services/api";
import { crearOperador, desactivarOperador } from "../../services/operadorApi";

const mockFetch = vi.mocked(fetchWithAuth);

describe("ApiError (operadorApi)", () => {
  it("creates error with status and body", () => {
    const err = new ApiError(403, "Forbidden");
    expect(err.status).toBe(403);
    expect(err.body).toBe("Forbidden");
    expect(err).toBeInstanceOf(Error);
  });
});

describe("operadorApi", () => {
  beforeEach(() => vi.clearAllMocks());

  it("crearOperador posts to /api/admin/operators and returns result", async () => {
    const mock = { id: "op-1", name: "Alice", email: "alice@test.com" };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await crearOperador({ name: "Alice", email: "alice@test.com" });

    expect(result.name).toBe("Alice");
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/admin/operators",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("crearOperador throws ApiError on 409 conflict", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 409, text: () => Promise.resolve("Conflict") } as Response);
    await expect(crearOperador({ name: "A", email: "dup@test.com" })).rejects.toThrow(ApiError);
  });

  it("desactivarOperador posts to /api/admin/operators/disable", async () => {
    const mock = { message: "Operador desactivado", wasAlreadyDisabled: false };
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(mock) } as Response);

    const result = await desactivarOperador({ email: "op@test.com" });

    expect(result.wasAlreadyDisabled).toBe(false);
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/admin/operators/disable",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("desactivarOperador throws ApiError on failure", async () => {
    mockFetch.mockResolvedValue({ ok: false, status: 404, text: () => Promise.resolve("Not found") } as Response);
    await expect(desactivarOperador({ email: "ghost@test.com" })).rejects.toThrow(ApiError);
  });
});
