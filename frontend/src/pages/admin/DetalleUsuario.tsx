import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getUserById, ApiError } from "../../services/adminUsuariosApi";
import type { UserDetailResponse } from "../../types/usuario";

const containerStyle: React.CSSProperties = {
  padding: "1rem",
  maxWidth: 700,
  margin: "0 auto",
};

const backButtonStyle: React.CSSProperties = {
  display: "inline-flex",
  alignItems: "center",
  gap: 6,
  padding: "6px 14px",
  border: "none",
  borderRadius: 4,
  cursor: "pointer",
  fontSize: 13,
  fontWeight: 600,
  backgroundColor: "#6c757d",
  color: "#fff",
  marginBottom: 16,
};

const cardStyle: React.CSSProperties = {
  border: "1px solid #dee2e6",
  borderRadius: 8,
  padding: "24px",
  backgroundColor: "#fff",
};

const sectionTitleStyle: React.CSSProperties = {
  fontSize: 16,
  fontWeight: 700,
  color: "#212529",
  marginBottom: 16,
  paddingBottom: 8,
  borderBottom: "2px solid #dee2e6",
};

const rowStyle: React.CSSProperties = {
  display: "flex",
  justifyContent: "space-between",
  alignItems: "flex-start",
  padding: "8px 0",
  borderBottom: "1px solid #f8f9fa",
  fontSize: 14,
};

const labelStyle: React.CSSProperties = {
  fontWeight: 600,
  color: "#495057",
  flex: "0 0 140px",
};

const valueStyle: React.CSSProperties = {
  color: "#212529",
  wordBreak: "break-word",
};

const badgeStyle = (color: string): React.CSSProperties => ({
  display: "inline-block",
  padding: "3px 10px",
  borderRadius: 12,
  fontSize: 12,
  fontWeight: 600,
  color: "#fff",
  backgroundColor: color,
});

const enabledStyle = badgeStyle("#28a745");
const disabledStyle = badgeStyle("#dc3545");
const verifiedStyle = badgeStyle("#28a745");
const unverifiedStyle = badgeStyle("#dc3545");

const errorStyle: React.CSSProperties = {
  padding: "12px 16px",
  border: "1px solid #dc3545",
  borderRadius: 4,
  backgroundColor: "#fff5f5",
  color: "#dc3545",
  fontSize: 14,
};

const notFoundStyle: React.CSSProperties = {
  textAlign: "center",
  padding: "40px",
  color: "#666",
  fontSize: 16,
};

function formatDate(dateStr: string): string {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  return d.toLocaleDateString("es-AR", {
    day: "2-digit",
    month: "long",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function DetalleUsuario() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [user, setUser] = useState<UserDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    setLoading(true);
    setError(null);

    getUserById(id)
      .then(u => { if (!cancelled) { setUser(u); setLoading(false); } })
      .catch(err => {
        if (cancelled) return;
        if (err instanceof ApiError) {
          setError(`Error ${err.status}: No se pudo cargar el usuario.`);
        } else {
          setError("Error de conexión.");
        }
        setLoading(false);
      });

    return () => { cancelled = true; };
  }, [id]);

  if (loading) {
    return (
      <div style={containerStyle}>
        <div style={{ color: "#666", fontSize: 14 }}>Cargando perfil...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div style={containerStyle}>
        <button style={backButtonStyle} onClick={() => navigate("/admin/usuarios")}>
          ← Volver
        </button>
        <div style={errorStyle}>{error}</div>
      </div>
    );
  }

  if (!user) {
    return (
      <div style={containerStyle}>
        <button style={backButtonStyle} onClick={() => navigate("/admin/usuarios")}>
          ← Volver
        </button>
        <div style={notFoundStyle}>Usuario no encontrado</div>
      </div>
    );
  }

  return (
    <div style={containerStyle}>
      <button style={backButtonStyle} onClick={() => navigate("/admin/usuarios")}>
        ← Volver al listado
      </button>

      <div style={cardStyle}>
        <h2 style={{ marginBottom: 20, fontSize: 20, fontWeight: 700 }}>
          Perfil de Usuario
        </h2>

        {/* Basic Info */}
        <div style={sectionTitleStyle}>Información General</div>

        <div style={rowStyle}>
          <span style={labelStyle}>ID</span>
          <span style={{ ...valueStyle, fontFamily: "monospace", fontSize: 12 }}>{user.id}</span>
        </div>
        <div style={rowStyle}>
          <span style={labelStyle}>Nombre</span>
          <span style={valueStyle}>{user.name}</span>
        </div>
        <div style={rowStyle}>
          <span style={labelStyle}>Email</span>
          <span style={valueStyle}>{user.email}</span>
        </div>
        <div style={rowStyle}>
          <span style={labelStyle}>Email verificado</span>
          <span style={valueStyle}>
            <span style={user.emailVerified ? verifiedStyle : unverifiedStyle}>
              {user.emailVerified ? "Sí" : "No"}
            </span>
          </span>
        </div>
        <div style={rowStyle}>
          <span style={labelStyle}>Estado</span>
          <span style={valueStyle}>
            <span style={user.enabled ? enabledStyle : disabledStyle}>
              {user.enabled ? "Activo" : "Inactivo"}
            </span>
          </span>
        </div>
        <div style={{ ...rowStyle, borderBottom: "none" }}>
          <span style={labelStyle}>Fecha de creación</span>
          <span style={valueStyle}>{formatDate(user.createdAt)}</span>
        </div>

        {/* Roles */}
        <div style={{ ...sectionTitleStyle, marginTop: 24 }}>Roles</div>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          {user.roles.length === 0 ? (
            <span style={{ color: "#666", fontSize: 14 }}>Sin roles asignados</span>
          ) : (
            user.roles.map(r => (
              <span
                key={r}
                style={badgeStyle(r === "admin" ? "#6610f2" : r === "operator" ? "#17a2b8" : "#28a745")}
              >
                {r}
              </span>
            ))
          )}
        </div>

        {/* Attributes */}
        {user.attributes && Object.keys(user.attributes).length > 0 && (
          <>
            <div style={{ ...sectionTitleStyle, marginTop: 24 }}>Atributos</div>
            {Object.entries(user.attributes).map(([key, values]) => (
              <div key={key} style={rowStyle}>
                <span style={labelStyle}>{key}</span>
                <span style={valueStyle}>{values.join(", ") || "—"}</span>
              </div>
            ))}
          </>
        )}
      </div>
    </div>
  );
}