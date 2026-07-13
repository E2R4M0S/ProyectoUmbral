import React, { Suspense, useState, useEffect, useRef, useCallback } from 'react';
import { BrowserRouter, Routes, Route, useNavigate, Outlet, Link, Navigate } from "react-router-dom";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { Registro } from "./pages/public/Registro";
import { ServerSetup } from "./pages/public/ServerSetup";
import { needsServerSetup, getServerHost, isProductionApk } from "./config/serverConfig";
import { MiPerfil } from "./pages/participant/MiPerfil";
import { UnirseEquipo } from "./pages/participant/UnirseEquipo";
import { QuizBank } from "./pages/admin/QuizBank";
import { UnirseSesion } from "./pages/participant/UnirseSesion";
import { GameView } from "./pages/participant/game/GameView";
import { EscanearQr } from "./pages/participant/game/EscanearQr";
import { MisionCompletada } from "./pages/participant/game/MisionCompletada";
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
import { PanelSesion } from "./pages/admin/PanelSesion";
import { ListadoSesiones } from "./pages/admin/ListadoSesiones";
import { DetalleMision } from "./pages/admin/DetalleMision";

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

  if (needsServerSetup()) {
    return <Navigate to="/setup" replace />;
  }

  if (auth.isLoading) {
    return (
      <div className="landing">
        <div className="spinner" />
      </div>
    );
  }

  if (auth.isAuthenticated) {
    const roles = auth.user?.access_token ? getRoles(auth.user.access_token) : [];
    if (roles.includes("admin")) return <Navigate to="/admin" replace />;
    if (roles.includes("operator")) return <Navigate to="/operator" replace />;
    return <Navigate to="/participant" replace />;
  }

  return (
    <div className="landing">
      <div className="landing-logo">U</div>
      <h1 className="landing-title">UMBRAL</h1>
      <p className="landing-subtitle">
        Plataforma de experiencias de investigación inmersiva
      </p>
      <button className="btn btn-primary btn-lg" onClick={() => auth.signinRedirect()}>
        Iniciar Sesión
      </button>
      <p style={{ marginTop: "1.5rem", fontSize: "0.875rem", color: "var(--text-muted)" }}>
        ¿No tenés cuenta?{" "}
        <Link to="/registro">Registrate</Link>
      </p>
      {isProductionApk && (
        <p style={{ marginTop: "1rem", fontSize: "0.75rem", color: "#555" }}>
          Servidor: {getServerHost() || "no configurado"}{" "}
          <Link to="/setup" style={{ color: "#e94560" }}>cambiar</Link>
        </p>
      )}
    </div>
  );
}

function Sidebar({ children, role }: { children: React.ReactNode; role: string }) {
  const auth = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const sidebarRef = useRef<HTMLDivElement>(null);

  const close = useCallback(() => setIsOpen(false), []);

  // Close on outside click (overlay + hamburger)
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (!isOpen) return;
      const target = e.target as HTMLElement;
      // Ignore clicks inside the sidebar
      if (sidebarRef.current?.contains(target)) return;
      // Ignore clicks on the hamburger button (it toggles on its own)
      if (target.closest(".hamburger-btn")) return;
      close();
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, [isOpen, close]);

  // Close on Escape key
  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "Escape") close();
    }
    if (isOpen) {
      document.addEventListener("keydown", handleKey);
      return () => document.removeEventListener("keydown", handleKey);
    }
  }, [isOpen, close]);

  const displayName = auth.user?.profile?.name
    ?? auth.user?.profile?.preferred_username
    ?? auth.user?.profile?.email
    ?? "Usuario";

  return (
    <div className="sidebar-layout">
      {/* Hamburger — mobile only */}
      <button
        className="hamburger-btn"
        onClick={() => setIsOpen((v) => !v)}
        aria-label={isOpen ? "Cerrar menú" : "Abrir menú"}
      >
        {isOpen ? "✕" : "☰"}
      </button>

      {/* Overlay */}
      <div
        className={`sidebar-overlay${isOpen ? " open" : ""}`}
        onClick={close}
      />

      {/* Sidebar */}
      <div ref={sidebarRef} className={`sidebar${isOpen ? " open" : ""}`}>
        <div className="sidebar-header">
          <div className="sidebar-brand">
            <div className="sidebar-brand-icon">U</div>
            <span className="sidebar-brand-name">UMBRAL</span>
          </div>
          <div className="sidebar-user">
            <div className="sidebar-user-name">{displayName}</div>
            <div className="sidebar-user-role">{role}</div>
          </div>
        </div>
        <nav className="sidebar-nav" onClick={close}>
          {children}
        </nav>
        <div className="sidebar-footer">
          <button onClick={() => auth.signoutRedirect()} className="sidebar-logout">
            Cerrar Sesión
          </button>
        </div>
      </div>

      <div className="main-content">
        <Outlet />
      </div>
    </div>
  );
}

function AdminPanel() {
  return (
    <Sidebar role="Administrador">
      <div className="sidebar-section">Misiones</div>
      <Link to="/admin/misiones" className="sidebar-link">📋 Catálogo</Link>
      <Link to="/admin/misiones/crear" className="sidebar-link">➕ Crear Misión</Link>
      <div className="sidebar-section">Trivia</div>
      <Link to="/admin/quiz" className="sidebar-link">📝 Banco de Preguntas</Link>
      <div className="sidebar-section">Sesiones</div>
      <Link to="/admin/sesiones" className="sidebar-link">📋 Listado</Link>
      <Link to="/admin/sesiones/crear" className="sidebar-link">➕ Crear Sesión</Link>
      <div className="sidebar-section">Equipos</div>
      <Link to="/admin/equipos" className="sidebar-link">📋 Listado</Link>
      <Link to="/admin/equipos/crear" className="sidebar-link">➕ Crear Equipo</Link>
      <div className="sidebar-section">Usuarios</div>
      <Link to="/admin/usuarios" className="sidebar-link">📋 Listado</Link>
      <Link to="/admin/operadores/nuevo" className="sidebar-link">➕ Crear Operador</Link>
      <Link to="/admin/operadores/desactivar" className="sidebar-link">🚫 Desactivar Operador</Link>
    </Sidebar>
  );
}

function OperatorPanel() {
  return (
    <Sidebar role="Operador">
      <div className="sidebar-section">Misiones</div>
      <Link to="/operator/misiones" className="sidebar-link">📋 Catálogo</Link>
      <div className="sidebar-section">Trivia</div>
      <Link to="/operator/quiz" className="sidebar-link">📝 Banco de Preguntas</Link>
      <div className="sidebar-section">Sesiones</div>
      <Link to="/operator/sesiones" className="sidebar-link">📋 Listado</Link>
      <Link to="/operator/sesiones/crear" className="sidebar-link">➕ Crear Sesión</Link>
      <div className="sidebar-section">Equipos</div>
      <Link to="/operator/equipos" className="sidebar-link">📋 Listado</Link>
      <Link to="/operator/equipos/crear" className="sidebar-link">➕ Crear Equipo</Link>
      <div className="sidebar-section">Usuarios</div>
      <Link to="/operator/usuarios" className="sidebar-link">📋 Listado</Link>
    </Sidebar>
  );
}

function ParticipantPanel() {
  return (
    <Sidebar role="Participante">
      <div className="sidebar-section">Mi Cuenta</div>
      <Link to="/participant/perfil" className="sidebar-link">👤 Mi Perfil</Link>
      <div className="sidebar-section">Equipos</div>
      <Link to="/participant/equipo/unirse" className="sidebar-link">🔗 Unirse a Equipo</Link>
      <div className="sidebar-section">Juego</div>
      <Link to="/participant/sessions/join" className="sidebar-link">🎮 Unirse a Sesión</Link>
    </Sidebar>
  );
}

function Dashboard() {
  return <h1>Dashboard — Autenticado</h1>;
}

function Callback() {
  const auth = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (auth.isAuthenticated) {
      const roles = auth.user?.access_token ? getRoles(auth.user.access_token) : [];
      if (roles.includes("admin")) navigate("/admin", { replace: true });
      else if (roles.includes("operator")) navigate("/operator", { replace: true });
      else navigate("/participant", { replace: true });
    }
  }, [auth.isAuthenticated, navigate]);

  if (auth.error) {
    // Clear URL params so a stale code doesn't cause a loop on reload
    window.history.replaceState({}, document.title, "/");
    return (
      <div style={{ padding: "2rem", textAlign: "center", color: "white" }}>
        <p style={{ color: "#e94560", fontWeight: 700 }}>Error de autenticación</p>
        <p style={{ color: "#aaa", fontSize: 13, margin: "0.5rem 0 1.5rem" }}>{auth.error.message}</p>
        <button
          onClick={() => navigate("/", { replace: true })}
          style={{ padding: "10px 24px", backgroundColor: "#e94560", color: "white", border: "none", borderRadius: 8, cursor: "pointer" }}
        >
          Volver al inicio
        </button>
      </div>
    );
  }

  return <div style={{ padding: "2rem", textAlign: "center", color: "#aaa" }}>Completando inicio de sesión...</div>;
}


function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/setup" element={<ServerSetup />} />
        <Route path="/callback" element={<Callback />} />
        <Route path="/registro" element={<Registro />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/misiones" element={<CatalogoMisiones />} />
        </Route>
        <Route path="/admin" element={<ProtectedRoute requiredRole="admin" />}>
          <Route element={<AdminPanel />}>
            <Route index element={<h2>Bienvenido al Panel de Administración</h2>} />
            <Route path="misiones" element={<CatalogoMisiones />} />
            <Route path="misiones/crear" element={<CrearMision />} />
            <Route path="misiones/:id/editar" element={<EditarMision />} />
            <Route path="misiones/:id" element={<DetalleMision />} />
            <Route path="quiz" element={<QuizBank />} />
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
            <Route path="sesiones/:id/panel" element={<PanelSesion />} />
          </Route>
        </Route>
          <Route path="/operator" element={<ProtectedRoute requiredRole="operator" />}>
            <Route element={<OperatorPanel />}>
              <Route index element={<h2>Panel de Operador</h2>} />
              <Route path="question-results" element={
                <Suspense fallback={<div>Cargando resultados...</div>}>
                  {React.createElement(React.lazy(() => import('./pages/operator/QuestionResults')) as any)}
                </Suspense>
              } />
            <Route path="misiones" element={<CatalogoMisiones />} />
            <Route path="quiz" element={<QuizBank />} />
            <Route path="sesiones" element={<ListadoSesiones />} />
            <Route path="sesiones/crear" element={<CrearSesion />} />
            <Route path="sesiones/:id/panel" element={<PanelSesion />} />
            <Route path="equipos" element={<ListadoEquipos />} />
            <Route path="equipos/crear" element={<CrearEquipo />} />
            <Route path="equipos/:id" element={<EquipoDetalle />} />
            <Route path="equipos/:id/editar" element={<EditarEquipo />} />
            <Route path="usuarios" element={<ListadoUsuarios />} />
          </Route>
        </Route>
        <Route path="/participant" element={<ProtectedRoute requiredRole="participant" />}>
          <Route element={<ParticipantPanel />}>
            <Route index element={<h2>Panel de Participante</h2>} />
            <Route path="perfil" element={<MiPerfil />} />
            <Route path="equipo/unirse" element={<UnirseEquipo />} />
            <Route path="sessions/join" element={<UnirseSesion />} />
          </Route>
        </Route>
        <Route path="/juego/:sessionId" element={<GameView />} />
        <Route path="/juego/:sessionId/escanear" element={<EscanearQr />} />
        <Route path="/juego/:sessionId/completada" element={<MisionCompletada />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
