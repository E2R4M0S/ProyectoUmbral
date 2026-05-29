import { UserManager, UserManagerSettings } from "oidc-client-ts";

const authority = window.location.hostname === "localhost"
  ? "http://localhost:8080/realms/umbral"
  : `http://${window.location.hostname}:8080/realms/umbral`;

const origin = window.location.origin;
const clientId = "umbral-frontend";

export const keycloakConfig: UserManagerSettings = {
  authority,
  client_id: clientId,
  redirect_uri: `${origin}/callback`,
  post_logout_redirect_uri: origin,
  response_type: "code",
  scope: "openid profile email roles",
  extraQueryParams: { audience: "umbral-gateway" },
  automaticSilentRenew: false,
  silent_redirect_uri: `${origin}/silent-renew.html`,
  disablePKCE: true,
};

export const userManager = new UserManager(keycloakConfig);
