import { AuthProvider as OidcAuthProvider } from "react-oidc-context";
import { userManager } from "./keycloak";
import type { ReactNode } from "react";

const onSigninCallback = () => {
  window.history.replaceState({}, document.title, window.location.pathname);
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
