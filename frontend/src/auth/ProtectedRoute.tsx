import { useAuth } from "./useAuth";
import { Outlet, Navigate, Link } from "react-router-dom";
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
    return <Navigate to="/" replace />;
  }

  if (requiredRole && auth.user?.access_token) {
    const roles = getRoles(auth.user.access_token);
    if (!roles.includes(requiredRole)) {
      return (
        <div style={{ padding: "4rem 2rem", textAlign: "center", color: "white", backgroundColor: "#1a1a2e", minHeight: "100vh", fontFamily: "sans-serif" }}>
          <h2 style={{ color: "#e94560", fontSize: "2rem" }}>403 - Acceso denegado</h2>
          <p style={{ color: "#ccc", marginBottom: "2rem" }}>
            No tenés el rol requerido (<strong>{requiredRole}</strong>) para acceder a esta página.
          </p>
          <p style={{ color: "#999", fontSize: "0.9rem", marginBottom: "2rem" }}>
            Tus roles: {roles.length > 0 ? roles.join(", ") : "ninguno"}
          </p>
          <div style={{ display: "flex", gap: "1rem", justifyContent: "center", flexWrap: "wrap" }}>
            <Link to="/" style={{ padding: "10px 24px", backgroundColor: "#0f3460", color: "white", textDecoration: "none", borderRadius: 6, fontWeight: 600 }}>
              Volver al inicio
            </Link>
            <button onClick={() => auth.signoutRedirect()} style={{ padding: "10px 24px", backgroundColor: "#e94560", color: "white", border: "none", borderRadius: 6, fontWeight: 600, cursor: "pointer" }}>
              Cerrar Sesión
            </button>
          </div>
        </div>
      );
    }
  }

  if (children) {
    return <>{children}</>;
  }

  return <Outlet />;
}
