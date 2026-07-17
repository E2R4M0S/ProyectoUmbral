import { CapacitorHttp } from "@capacitor/core";
import { isProductionApk, buildApiBase } from "../config/serverConfig";

interface RegistroRequest {
  firstName: string;
  lastName: string;
  username: string;
  alias: string;
  email: string;
  password: string;
}

interface RegistroResponse {
  id: string;
}

function regUrl(): string {
  const base = isProductionApk ? buildApiBase() : "";
  return `${base}/api/register`;
}

export async function registrarParticipante(
  data: RegistroRequest,
): Promise<RegistroResponse> {
  const url = regUrl();
  const body = JSON.stringify(data);
  const headers = { "Content-Type": "application/json" };

  if (isProductionApk) {
    const res = await CapacitorHttp.request({
      method: "POST",
      url,
      headers,
      data: body,
    });
    if (res.status < 200 || res.status >= 300) {
      const errBody = typeof res.data === "string" ? res.data : JSON.stringify(res.data ?? "");
      throw new ApiError(res.status, errBody);
    }
    return res.data as RegistroResponse;
  }

  const response = await fetch(url, {
    method: "POST",
    headers,
    body,
  });

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: string,
  ) {
    super(`API Error: ${status}`);
    this.name = "ApiError";
  }
}
