import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getUserById, ApiError } from "../../services/adminUsuariosApi";
import { activarOperador, desactivarOperador, ApiError as OperadorApiError } from "../../services/operadorApi";
import type { UserDetailResponse } from "../../types/usuario";
import { filterKnownRoles, roleClass, roleLabel } from "../../utils/roles";

function formatDate(dateStr: string): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("es-AR", {
    day: "2-digit", month: "long", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
}

export function DetalleUsuario() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [user, setUser]     = useState<UserDetailResponse | null>(null);
  const [loading, setLoad]  = useState(true);
  const [error, setError]   = useState<string | null>(null);
  const [statusMsg, setStatusMsg] = useState<{ text: string; type: "success" | "error" } | null>(null);
  const [togglingStatus, setTogglingStatus] = useState(false);

  function load() {
    if (!id) return;
    setLoad(true);
    setError(null);

    return getUserById(id)
      .then(u  => { setUser(u); setLoad(false); })
      .catch(err => {
        setError(err instanceof ApiError
          ? `Error ${err.status}: No se pudo cargar el usuario.`
          : "Error de conexión.");
        setLoad(false);
      });
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  async function toggleUserStatus() {
    if (!user) return;
    const action = user.enabled ? "desactivar" : "activar";
    if (!window.confirm(`¿${action.charAt(0).toUpperCase() + action.slice(1)} al usuario "${user.email}"?`)) return;

    setTogglingStatus(true);
    setStatusMsg(null);
    try {
      if (user.enabled) {
        await desactivarOperador({ email: user.email });
      } else {
        await activarOperador({ email: user.email });
      }
      setStatusMsg({ text: `Usuario ${user.enabled ? "desactivado" : "activado"} correctamente.`, type: "success" });
      await load();
    } catch (err) {
      const msg = err instanceof OperadorApiError
        ? `Error ${err.status}: no se pudo ${action} el usuario.`
        : "Error de conexión.";
      setStatusMsg({ text: msg, type: "error" });
    } finally {
      setTogglingStatus(false);
    }
  }

  if (loading) {
    return (
      <div className="page" style={{ textAlign: "center", paddingTop: "4rem" }}>
        <div className="spinner" style={{ margin: "0 auto" }} />
      </div>
    );
  }

  if (error) {
    return (
      <div className="page">
        <button className="btn btn-secondary" style={{ marginBottom: "1rem" }} onClick={() => navigate(-1)}>
          ← Volver
        </button>
        <div className="alert alert-error">{error}</div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="page">
        <button className="btn btn-secondary" style={{ marginBottom: "1rem" }} onClick={() => navigate(-1)}>
          ← Volver
        </button>
        <div className="empty-state">
          <div className="empty-state-icon">👤</div>
          <div className="empty-state-text">Usuario no encontrado</div>
        </div>
      </div>
    );
  }

  return (
    <div className="page" style={{ maxWidth: 700 }}>
      <button className="btn btn-secondary btn-sm" style={{ marginBottom: "1.5rem" }} onClick={() => navigate(-1)}>
        ← Volver al listado
      </button>

      <div className="card">
        <h2 className="card-title">Perfil de Usuario</h2>

        <p style={{ fontSize: "0.75rem", fontWeight: 700, textTransform: "uppercase", letterSpacing: "1px", color: "var(--text-muted)", marginBottom: "0.75rem" }}>
          Información General
        </p>

        <div className="detail-row">
          <span className="detail-label">ID</span>
          <span className="detail-value" style={{ fontFamily: "var(--font-mono)", fontSize: "0.75rem" }}>{user.id}</span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Nombre</span>
          <span className="detail-value">{user.name}</span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Email</span>
          <span className="detail-value">{user.email}</span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Email verificado</span>
          <span className="detail-value">
            <span className={user.emailVerified ? "badge badge-success" : "badge badge-error"}>
              {user.emailVerified ? "Verificado" : "Sin verificar"}
            </span>
          </span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Estado</span>
          <span className="detail-value">
            <span className={user.enabled ? "badge badge-success" : "badge badge-error"}>
              {user.enabled ? "Activo" : "Inactivo"}
            </span>
          </span>
        </div>
        <div className="detail-row">
          <span className="detail-label">Creación</span>
          <span className="detail-value">{formatDate(user.createdAt)}</span>
        </div>

        {!user.roles.includes("admin") && (
          <div style={{ marginTop: "1.25rem", borderTop: "1px solid var(--border)", paddingTop: "1.25rem" }}>
            {statusMsg && (
              <div className={statusMsg.type === "success" ? "alert alert-success" : "alert alert-error"} style={{ marginBottom: "0.75rem" }}>
                {statusMsg.text}
              </div>
            )}
            <button
              className={`btn btn-sm ${user.enabled ? "btn-danger" : "btn-success"}`}
              onClick={toggleUserStatus}
              disabled={togglingStatus}
            >
              {togglingStatus ? "Procesando..." : user.enabled ? "Desactivar Usuario" : "Activar Usuario"}
            </button>
          </div>
        )}

        <p style={{ fontSize: "0.75rem", fontWeight: 700, textTransform: "uppercase", letterSpacing: "1px", color: "var(--text-muted)", marginTop: "1.5rem", marginBottom: "0.75rem", borderTop: "1px solid var(--border)", paddingTop: "1.25rem" }}>
          Roles
        </p>
        <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
          {filterKnownRoles(user.roles).length === 0 ? (
            <span style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>Sin roles asignados</span>
          ) : (
            filterKnownRoles(user.roles).map(r => (
              <span key={r} className={roleClass(r)}>{roleLabel(r)}</span>
            ))
          )}
        </div>

        {user.attributes && Object.keys(user.attributes).length > 0 && (
          <>
            <p style={{ fontSize: "0.75rem", fontWeight: 700, textTransform: "uppercase", letterSpacing: "1px", color: "var(--text-muted)", marginTop: "1.5rem", marginBottom: "0.75rem", borderTop: "1px solid var(--border)", paddingTop: "1.25rem" }}>
              Atributos
            </p>
            {Object.entries(user.attributes).map(([key, values]) => (
              <div className="detail-row" key={key}>
                <span className="detail-label">{key}</span>
                <span className="detail-value">{values.join(", ") || "—"}</span>
              </div>
            ))}
          </>
        )}
      </div>
    </div>
  );
}
