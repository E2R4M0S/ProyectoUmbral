interface RegistroRequest {
  name: string;
  alias: string;
  email: string;
  password: string;
}

interface RegistroResponse {
  id: string;
}

/**
 * Registra un nuevo participante a través del endpoint público del Gateway.
 * No requiere autenticación — el proxy de Vite redirige a la API en desarrollo.
 */
export async function registrarParticipante(
  data: RegistroRequest,
): Promise<RegistroResponse> {
  const response = await fetch("/api/teams/register", {
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
