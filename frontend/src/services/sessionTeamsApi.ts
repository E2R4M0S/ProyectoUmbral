import { fetchWithAuth } from "./api";

export interface SessionTeamMember {
  userId: string;
  userAlias: string;
  joinedAt: string;
}

export interface SessionTeam {
  id: string;
  name: string;
  memberCount: number;
  maxMembers: number;
  members: SessionTeamMember[];
}

class ApiError extends Error {
  constructor(public status: number, public body: string) {
    super(`API Error: ${status}`);
  }
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const body = await res.text().catch(() => "");
    throw new ApiError(res.status, body);
  }
  return res.json();
}

export async function getSessionTeams(sessionId: string): Promise<SessionTeam[]> {
  const res = await fetchWithAuth(`/api/sessions/${sessionId}/teams`);
  if (!res.ok) return [];
  return res.json();
}

export async function createSessionTeam(sessionId: string, name: string): Promise<SessionTeam> {
  const res = await fetchWithAuth(`/api/sessions/${sessionId}/teams`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name }),
  });
  return handleResponse<SessionTeam>(res);
}

export async function joinSessionTeam(sessionId: string, teamId: string): Promise<void> {
  const res = await fetchWithAuth(`/api/sessions/${sessionId}/teams/${teamId}/join`, {
    method: "POST",
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.message || "No se pudo unir al equipo.");
  }
}

export async function removeTeamMember(sessionId: string, teamId: string, userId: string): Promise<void> {
  const res = await fetchWithAuth(`/api/sessions/${sessionId}/teams/${teamId}/members/${userId}`, {
    method: "DELETE",
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.message || "No se pudo eliminar al miembro.");
  }
}
