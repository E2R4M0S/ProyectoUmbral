import { useAuth } from "./useAuth";
import { Outlet } from "react-router-dom";
import type { ReactNode } from "react";

interface ProtectedRouteProps {
  requiredRole?: string;
  children?: ReactNode;
}

function getRoles(accessToken: string): string[] {
  try {
    const payload = JSON.parse(atob(accessToken.split(".")[1]));
    return payload.realm_access?.roles ?? [];
  } catch {
    return [];
  }
}

export function ProtectedRoute({ requiredRole, children }: ProtectedRouteProps) {
  const auth = useAuth();

  if (auth.isLoading) {
    return <div>Cargando autenticación...</div>;
  }

  if (!auth.isAuthenticated) {
    auth.signinRedirect({
      extraQueryParams: { audience: "umbral-gateway" },
    });
    return null;
  }

  if (requiredRole && auth.user?.access_token) {
    const roles = getRoles(auth.user.access_token);
    if (!roles.includes(requiredRole)) {
      return (
        <div style={{ padding: "2rem", textAlign: "center" }}>
          <h2>403 - Acceso denegado</h2>
          <p>
            No tenés el rol requerido (<strong>{requiredRole}</strong>) para
            acceder a esta página.
          </p>
        </div>
      );
    }
  }

  if (children) {
    return <>{children}</>;
  }

  return <Outlet />;
}
