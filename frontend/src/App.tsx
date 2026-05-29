import { BrowserRouter, Routes, Route, useNavigate, Outlet } from "react-router-dom";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { Registro } from "./pages/public/Registro";
import { MiPerfil } from "./pages/participant/MiPerfil";
import { UnirseEquipo } from "./pages/participant/UnirseEquipo";
import { CrearOperador } from "./pages/admin/CrearOperador";
import { DesactivarOperador } from "./pages/admin/DesactivarOperador";
import { ListadoUsuarios } from "./pages/admin/ListadoUsuarios";
import { DetalleUsuario } from "./pages/admin/DetalleUsuario";
import { CrearMision } from "./pages/admin/CrearMision";
import { EditarMision } from "./pages/admin/EditarMision";
import { CatalogoMisiones } from "./pages/admin/CatalogoMisiones";
import { CrearEquipo } from "./pages/admin/CrearEquipo";
import { ListadoEquipos } from "./pages/admin/ListadoEquipos";
import { EquipoDetalle } from "./pages/admin/EquipoDetalle";
import { EditarEquipo } from "./pages/admin/EditarEquipo";
import { CrearSesion } from "./pages/admin/CrearSesion";
import { ListadoSesiones } from "./pages/admin/ListadoSesiones";

import { useAuth } from "react-oidc-context";

function getRoles(accessToken: string): string[] {
  try {
    const payload = JSON.parse(atob(accessToken.split(".")[1]));
    return payload.realm_access?.roles ?? [];
  } catch {
    return [];
  }
}

function Home() {
  const auth = useAuth();

  if (auth.isLoading) {
    return <div style={{ padding: "2rem", textAlign: "center" }}>Cargando...</div>;
  }

  if (auth.isAuthenticated) {
    const roles = auth.user?.access_token ? getRoles(auth.user.access_token) : [];
    if (roles.includes("admin")) return <AdminPanel><h2>Bienvenido</h2></AdminPanel>;
    if (roles.includes("operator")) return <OperatorPanel><h2>Bienvenido</h2></OperatorPanel>;
    return <ParticipantPanel><h2>Bienvenido</h2></ParticipantPanel>;
  }

  return (
    <div style={{ 
      display: "flex", flexDirection: "column", alignItems: "center", 
      justifyContent: "center", height: "100vh", fontFamily: "sans-serif",
      backgroundColor: "#1a1a2e", color: "white"
    }}>
      <h1 style={{ fontSize: "3rem", marginBottom: "0.5rem" }}>UMBRAL</h1>
      <p style={{ fontSize: "1.2rem", color: "#aaa", marginBottom: "2rem" }}>
        Plataforma de experiencias de investigación inmersiva
      </p>
      <button onClick={() => auth.signinRedirect()} style={{
        padding: "12px 32px", backgroundColor: "#e94560", color: "white",
        border: "none", borderRadius: "8px", fontSize: "1.1rem",
        fontWeight: "bold", cursor: "pointer"
      }}>
        Iniciar Sesión
      </button>
    </div>
  );
}

const sidebarWidth = 240;

const sidebarStyle: React.CSSProperties = {
  width: sidebarWidth, minHeight: "100vh", backgroundColor: "#16213e",
  padding: "20px 0", position: "fixed", left: 0, top: 0,
  borderRight: "2px solid #e94560", display: "flex", flexDirection: "column",
};

const sidebarLinkStyle: React.CSSProperties = {
  color: "#ccc", textDecoration: "none", padding: "12px 24px",
  display: "block", fontSize: "0.95rem",
};

const sidebarSectionStyle: React.CSSProperties = {
  color: "#e94560", fontSize: "0.75rem", fontWeight: 600,
  textTransform: "uppercase", letterSpacing: 1, padding: "20px 24px 8px",
};

function Sidebar({ children, role }: { children: React.ReactNode; role: string }) {
  const auth = useAuth();
  return (
    <div style={{ display: "flex", fontFamily: "sans-serif" }}>
      <div style={sidebarStyle}>
        <div style={{ padding: "0 24px 20px", borderBottom: "1px solid #0f3460", marginBottom: 8 }}>
          <h2 style={{ color: "#e94560", margin: 0, fontSize: "1.3rem" }}>UMBRAL</h2>
          <p style={{ color: "#ccc", fontSize: "0.85rem", margin: "6px 0 0" }}>
            {auth.user?.profile?.name ?? auth.user?.profile?.preferred_username ?? auth.user?.profile?.email}
          </p>
          <p style={{ color: "#999", fontSize: "0.7rem", margin: "2px 0 0", textTransform: "uppercase", letterSpacing: 1 }}>{role}</p>
        </div>
        {children}
        <div style={{ marginTop: "auto", padding: "0 24px 20px" }}>
          <button onClick={() => auth.signoutRedirect()} style={{
            width: "100%", padding: "10px", backgroundColor: "#e94560",
            color: "white", border: "none", borderRadius: 6,
            cursor: "pointer", fontWeight: 600, fontSize: "0.9rem",
          }}>
            Cerrar Sesión
          </button>
        </div>
      </div>
      <div style={{ marginLeft: sidebarWidth, padding: "2rem", backgroundColor: "#1a1a2e", minHeight: "100vh", color: "white", flex: 1 }}>
        <Outlet />
      </div>
    </div>
  );
}

function AdminPanel() {
  return (
    <Sidebar role="Administrador">
      <div style={sidebarSectionStyle}>Misiones</div>
      <a href="misiones" style={sidebarLinkStyle}>📋 Catálogo</a>
      <a href="misiones/crear" style={sidebarLinkStyle}>➕ Crear Misión</a>
      <div style={sidebarSectionStyle}>Sesiones</div>
      <a href="sesiones" style={sidebarLinkStyle}>📋 Listado</a>
      <a href="sesiones/crear" style={sidebarLinkStyle}>➕ Crear Sesión</a>
      <div style={sidebarSectionStyle}>Equipos</div>
      <a href="equipos" style={sidebarLinkStyle}>📋 Listado</a>
      <a href="equipos/crear" style={sidebarLinkStyle}>➕ Crear Equipo</a>
      <div style={sidebarSectionStyle}>Usuarios</div>
      <a href="usuarios" style={sidebarLinkStyle}>📋 Listado</a>
      <a href="operadores/nuevo" style={sidebarLinkStyle}>➕ Crear Operador</a>
    </Sidebar>
  );
}

function OperatorPanel() {
  return (
    <Sidebar role="Operador">
      <div style={sidebarSectionStyle}>Misiones</div>
      <a href="misiones" style={sidebarLinkStyle}>📋 Catálogo</a>
      <div style={sidebarSectionStyle}>Sesiones</div>
      <a href="sesiones" style={sidebarLinkStyle}>📋 Listado</a>
      <a href="sesiones/crear" style={sidebarLinkStyle}>➕ Crear Sesión</a>
      <div style={sidebarSectionStyle}>Equipos</div>
      <a href="equipos" style={sidebarLinkStyle}>📋 Listado</a>
    </Sidebar>
  );
}

function ParticipantPanel() {
  return (
    <Sidebar role="Participante">
      <div style={sidebarSectionStyle}>Mi Cuenta</div>
      <a href="perfil" style={sidebarLinkStyle}>👤 Mi Perfil</a>
      <div style={sidebarSectionStyle}>Equipos</div>
      <a href="equipo/unirse" style={sidebarLinkStyle}>🔗 Unirse a Equipo</a>
    </Sidebar>
  );
}

function Dashboard() {
  return <h1>Dashboard — Autenticado</h1>;
}

function Callback() {
  const auth = useAuth();
  const navigate = useNavigate();

  if (auth.isAuthenticated) {
    const roles = auth.user?.access_token ? getRoles(auth.user.access_token) : [];
    if (roles.includes("admin")) navigate("/", { replace: true });
    else if (roles.includes("operator")) navigate("/", { replace: true });
    else navigate("/participant", { replace: true });
    return null;
  }

  if (auth.error) {
    return <div>Error: {auth.error.message}</div>;
  }

  return <div>Completando inicio de sesión...</div>;
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/callback" element={<Callback />} />
        <Route path="/registro" element={<Registro />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/misiones" element={<CatalogoMisiones />} />
        </Route>
        <Route element={<ProtectedRoute requiredRole="admin" />}>
          <Route element={<AdminPanel />}>
            <Route index element={<h2>Bienvenido al Panel de Administración</h2>} />
            <Route path="misiones" element={<CatalogoMisiones />} />
            <Route path="misiones/crear" element={<CrearMision />} />
            <Route path="misiones/:id/editar" element={<EditarMision />} />
            <Route path="usuarios" element={<ListadoUsuarios />} />
            <Route path="usuarios/:id" element={<DetalleUsuario />} />
            <Route path="operadores/nuevo" element={<CrearOperador />} />
            <Route path="operadores/desactivar" element={<DesactivarOperador />} />
            <Route path="equipos" element={<ListadoEquipos />} />
            <Route path="equipos/crear" element={<CrearEquipo />} />
            <Route path="equipos/:id" element={<EquipoDetalle />} />
            <Route path="equipos/:id/editar" element={<EditarEquipo />} />
            <Route path="sesiones" element={<ListadoSesiones />} />
            <Route path="sesiones/crear" element={<CrearSesion />} />
          </Route>
        </Route>
        <Route element={<ProtectedRoute requiredRole="operator" />}>
          <Route element={<OperatorPanel />}>
            <Route index element={<h2>Panel de Operador</h2>} />
            <Route path="misiones" element={<CatalogoMisiones />} />
            <Route path="sesiones" element={<ListadoSesiones />} />
            <Route path="sesiones/crear" element={<CrearSesion />} />
            <Route path="equipos" element={<ListadoEquipos />} />
          </Route>
        </Route>
        <Route element={<ProtectedRoute requiredRole="participant" />}>
          <Route element={<ParticipantPanel />}>
            <Route index element={<h2>Panel de Participante</h2>} />
            <Route path="perfil" element={<MiPerfil />} />
            <Route path="equipo/unirse" element={<UnirseEquipo />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
