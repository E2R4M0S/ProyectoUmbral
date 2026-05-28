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

export interface DesactivarOperadorRequest {
  email: string;
}

export interface DesactivarOperadorResponse {
  message: string;
  wasAlreadyDisabled: boolean;
}
