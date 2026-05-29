export interface TeamResponse {
  id: string;
  name: string;
  description: string;
  leaderId: string;
  joinCode: string;
  createdAt: string;
}

export interface TeamListItem {
  id: string;
  name: string;
  memberCount: number;
  joinCode: string;
}

export interface TeamDetail {
  id: string;
  name: string;
  description: string;
  leaderId: string;
  leaderName: string;
  joinCode: string;
  members: TeamMember[];
  createdAt: string;
}

export interface TeamMember {
  id: string;
  userId: string;
  name: string;
  email: string;
  role: string;
  joinedAt: string;
}

export interface CreateTeamRequest {
  name: string;
  description: string;
  leaderId: string;
}

export interface UpdateTeamRequest {
  name?: string;
  description?: string;
  leaderId?: string;
}

export interface AddMemberRequest {
  userId: string;
}

export interface GetTeamsParams {
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface GetTeamsResponse {
  items: TeamListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}