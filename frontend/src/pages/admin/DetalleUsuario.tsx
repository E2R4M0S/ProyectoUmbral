import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getUserById, ApiError } from "../../services/adminUsuariosApi";
import type { UserDetailResponse } from "../../types/usuario";

function formatDate(dateStr: string): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("es-AR", {
    day: "2-digit", month: "long", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
}

function roleClass(r: string) {
  if (r === "admin")       return "badge badge-admin";
  if (r === "operator")    return "badge badge-operator";
  if (r === "participant") return "badge badge-participant";
  return "badge badge-muted";
}

export function DetalleUsuario() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [user, setUser]     = useState<UserDetailResponse | null>(null);
  const [loading, setLoad]  = useState(true);
  const [error, setError]   = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    setLoad(true);
    setError(null);

    getUserById(id)
      .then(u  => { if (!cancelled) { setUser(u); setLoad(false); } })
      .catch(err => {
        if (cancelled) return;
        setError(err instanceof ApiError
          ? `Error ${err.status}: No se pudo cargar el usuario.`
          : "Error de conexión.");
        setLoad(false);
      });

    return () => { cancelled = true; };
  }, [id]);

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

        <p style={{ fontSize: "0.75rem", fontWeight: 700, textTransform: "uppercase", letterSpacing: "1px", color: "var(--text-muted)", marginTop: "1.5rem", marginBottom: "0.75rem", borderTop: "1px solid var(--border)", paddingTop: "1.25rem" }}>
          Roles
        </p>
        <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
          {user.roles.length === 0 ? (
            <span style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>Sin roles asignados</span>
          ) : (
            user.roles.map(r => (
              <span key={r} className={roleClass(r)}>{r}</span>
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
