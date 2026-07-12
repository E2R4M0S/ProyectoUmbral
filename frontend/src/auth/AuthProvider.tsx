import { AuthProvider as OidcAuthProvider } from "react-oidc-context";
import { userManager } from "./keycloak";
import type { ReactNode } from "react";

const onSigninCallback = () => {
  // Always navigate to root — removes code/state params from URL so a WebView
  // reload doesn't re-attempt an already-used authorization code.
  window.history.replaceState({}, document.title, "/");
};

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  return (
    <OidcAuthProvider userManager={userManager} onSigninCallback={onSigninCallback}>
      {children}
    </OidcAuthProvider>
  );
}
