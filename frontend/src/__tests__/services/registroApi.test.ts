import { describe, it, expect, vi, beforeEach } from "vitest";
import { ApiError, registrarParticipante } from "../../services/registroApi";

// registroApi uses plain fetch (no auth), so mock globalThis.fetch
const mockFetch = vi.fn();
vi.stubGlobal("fetch", mockFetch);

describe("ApiError (registroApi)", () => {
  it("creates error with status and body", () => {
    const err = new ApiError(422, "Unprocessable");
    expect(err.status).toBe(422);
    expect(err.body).toBe("Unprocessable");
    expect(err).toBeInstanceOf(Error);
  });
});

describe("registrarParticipante", () => {
  beforeEach(() => vi.clearAllMocks());

  it("posts to /api/teams/register and returns id", async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ id: "new-user-id" }),
    });

    const result = await registrarParticipante({
      name: "John",
      alias: "jdoe",
      email: "john@test.com",
      password: "pass123",
    });

    expect(result.id).toBe("new-user-id");
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/teams/register",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("throws ApiError when response is not ok", async () => {
    mockFetch.mockResolvedValue({
      ok: false,
      status: 409,
      text: () => Promise.resolve("Email already taken"),
    });

    await expect(
      registrarParticipante({ name: "J", alias: "j", email: "dup@test.com", password: "p" })
    ).rejects.toThrow(ApiError);
  });

  it("sends correct content-type header", async () => {
    mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve({ id: "x" }) });

    await registrarParticipante({ name: "A", alias: "a", email: "a@b.com", password: "p" });

    const [, options] = mockFetch.mock.calls[0];
    expect(options.headers["Content-Type"]).toBe("application/json");
  });
});
