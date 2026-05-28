export interface CreateOperadorRequest {
  name: string;
  email: string;
  password: string;
}

export interface OperadorResponse {
  name: string;
  email: string;
  keycloakUserId: string;
}
