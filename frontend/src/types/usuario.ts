export interface UserListItem {
  id: string;
  name: string;
  email: string;
  roles: string[];
  enabled: boolean;
  createdAt: string;
}

export interface UserDetailResponse extends UserListItem {
  attributes: Record<string, string[]>;
  emailVerified: boolean;
}

export interface GetUsersResponse {
  items: UserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface GetUsersParams {
  search?: string;
  role?: string;
  enabled?: boolean;
  page?: number;
  pageSize?: number;
}