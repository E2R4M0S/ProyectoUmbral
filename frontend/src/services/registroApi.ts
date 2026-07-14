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

export async function registrarParticipante(
  data: RegistroRequest,
): Promise<RegistroResponse> {
  const response = await fetch("/api/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
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
