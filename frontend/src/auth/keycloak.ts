import { UserManager, UserManagerSettings, WebStorageStateStore } from "oidc-client-ts";
import { Capacitor } from "@capacitor/core";
import { buildKeycloakBase } from "../config/serverConfig";

const keycloakBase = buildKeycloakBase();
const isNative = Capacitor.isNativePlatform();
// Use a custom URL scheme in the APK so Android routes Keycloak's redirect as a
// deep link intent instead of trying a TCP connection to localhost.
const redirectUri = isNative ? "umbral://callback" : `${window.location.origin}/callback`;
const postLogoutUri = isNative ? "umbral://callback" : window.location.origin;
const realmPath = "/realms/umbral";
const oidcBase = `${realmPath}/protocol/openid-connect`;

export const keycloakConfig: UserManagerSettings = {
  authority: `${keycloakBase}${realmPath}`,
  client_id: "umbral-frontend",
  redirect_uri: redirectUri,
  post_logout_redirect_uri: postLogoutUri,
  response_type: "code",
  scope: "openid profile email roles",
  disablePKCE: true,
  automaticSilentRenew: false,
  // stateStore in localStorage so the OIDC code+state survives WebView navigation to Keycloak and back.
  // userStore in sessionStorage so stale tokens don't persist across app restarts.
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

export const userManager = new UserManager(keycloakConfig);
