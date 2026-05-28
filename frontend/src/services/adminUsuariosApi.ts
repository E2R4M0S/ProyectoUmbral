import { fetchWithAuth } from "./api";
import type { UserDetailResponse, GetUsersResponse, GetUsersParams } from "../types/usuario";

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: string,
  ) {
    super(`API Error: ${status}`);
    this.name = "ApiError";
  }
}

function buildQueryString(params: GetUsersParams): string {
  const parts: string[] = [];
  if (params.search !== undefined) parts.push(`search=${encodeURIComponent(params.search)}`);
  if (params.role !== undefined) parts.push(`role=${encodeURIComponent(params.role)}`);
  if (params.enabled !== undefined) parts.push(`enabled=${params.enabled}`);
  if (params.page !== undefined) parts.push(`page=${params.page}`);
  if (params.pageSize !== undefined) parts.push(`pageSize=${params.pageSize}`);
  return parts.length > 0 ? `?${parts.join("&")}` : "";
}

export async function getUsers(params: GetUsersParams = {}): Promise<GetUsersResponse> {
  const qs = buildQueryString(params);
  const response = await fetchWithAuth(`/api/admin/users${qs}`);

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}

export async function getUserById(id: string): Promise<UserDetailResponse> {
  const response = await fetchWithAuth(`/api/admin/users/${encodeURIComponent(id)}`);

  if (!response.ok) {
    const errorBody = await response.text().catch(() => "");
    throw new ApiError(response.status, errorBody);
  }

  return response.json();
}