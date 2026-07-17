export interface CreateOperadorRequest {
  name: string;
  email: string;
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

export interface ActivarOperadorRequest {
  email: string;
}

export interface ActivarOperadorResponse {
  message: string;
  wasAlreadyEnabled: boolean;
}
