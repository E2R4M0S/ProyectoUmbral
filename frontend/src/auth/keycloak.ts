import { UserManager, UserManagerSettings } from "oidc-client-ts";

const authority = "http://localhost:8080/realms/umbral";
const clientId = "umbral-frontend";
const redirectUri = "http://localhost:5173/callback";

export const keycloakConfig: UserManagerSettings = {
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: "http://localhost:5173",
  response_type: "code",
  scope: "openid profile email roles",
  extraQueryParams: { audience: "umbral-gateway" },
  automaticSilentRenew: true,
  silent_redirect_uri: "http://localhost:5173/silent-renew.html",
};

export const userManager = new UserManager(keycloakConfig);
