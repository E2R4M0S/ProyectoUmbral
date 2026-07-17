import { UserManager, UserManagerSettings, WebStorageStateStore } from "oidc-client-ts";
import { Capacitor } from "@capacitor/core";
import { buildKeycloakBase } from "../config/serverConfig";

const isNative = Capacitor.isNativePlatform();
const realmPath = "/realms/umbral";
const oidcBase = `${realmPath}/protocol/openid-connect`;

function buildSettings(): UserManagerSettings {
  const keycloakBase = buildKeycloakBase();
  const redirectUri = isNative ? "umbral://callback" : `${window.location.origin}/callback`;
  const postLogoutUri = isNative ? "umbral://callback" : window.location.origin;

  return {
    authority: `${keycloakBase}${realmPath}`,
    client_id: "umbral-frontend",
    redirect_uri: redirectUri,
    post_logout_redirect_uri: postLogoutUri,
    response_type: "code",
    scope: "openid profile email roles",
    disablePKCE: true,
    automaticSilentRenew: false,
    userStore: new WebStorageStateStore({ store: window.sessionStorage }),
    stateStore: new WebStorageStateStore({ store: window.localStorage }),
    metadata: {
      issuer: `${keycloakBase}${realmPath}`,
      authorization_endpoint: `${keycloakBase}${oidcBase}/auth`,
      end_session_endpoint: `${keycloakBase}${oidcBase}/logout`,
      token_endpoint: `${keycloakBase}${oidcBase}/token`,
      jwks_uri: `${keycloakBase}${oidcBase}/certs`,
      userinfo_endpoint: `${keycloakBase}${oidcBase}/userinfo`,
    },
  };
}

// Singleton UserManager that can be re-initialized if the server host changes
let _userManager: UserManager | null = null;

export function getUserManager(): UserManager {
  if (!_userManager) {
    _userManager = new UserManager(buildSettings());
  }
  return _userManager;
}

// Re-create the UserManager when the server host changes (e.g. after /setup saves a new IP)
export function reinitUserManager(): void {
  _userManager = new UserManager(buildSettings());
}

// Backwards-compatible singleton for imports like:
//   import { userManager } from "../auth/keycloak";
export const userManager = getUserManager();
