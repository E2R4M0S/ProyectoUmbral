import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getTeamById, removeMember, ApiError } from "../../services/teamsApi";
import type { TeamDetail } from "../../types/team";

const containerStyle: React.CSSProperties = {
  padding: "1rem",
  maxWidth: 800,
  margin: "0 auto",
};

const cardStyle: React.CSSProperties = {
  backgroundColor: "#16213e",
  borderRadius: 8,
  padding: "1.5rem",
  marginBottom: "1rem",
};

const labelStyle: React.CSSProperties = {
  color: "#aaa",
  fontSize: 12,
  marginBottom: 2,
};

const valueStyle: React.CSSProperties = {
  color: "white",
  fontSize: 16,
  marginBottom: 16,
};

const buttonStyle = (variant: "primary" | "danger" | "secondary"): React.CSSProperties => ({
  padding: "8px 16px",
  border: "none",
  borderRadius: 4,
  cursor: "pointer",
  fontSize: 14,
  fontWeight: 600,
  backgroundColor: variant === "primary" ? "#0f3460" : variant === "danger" ? "#e94560" : "#6c757d",
  color: "#fff",
  marginRight: 8,
});

const errorStyle: React.CSSProperties = {
  padding: "10px 14px",
  border: "1px solid #e94560",
  borderRadius: 4,
  backgroundColor: "#2d1a1a",
  color: "#e94560",
  marginBottom: 16,
  fontSize: 14,
};

const successStyle: React.CSSProperties = {
  padding: "10px 14px",
  border: "1px solid #28a745",
  borderRadius: 4,
  backgroundColor: "#1a4d1a",
  color: "#28a745",
  marginBottom: 16,
  fontSize: 14,
};

const memberRowStyle: React.CSSProperties = {
  display: "flex",
  justifyContent: "space-between",
  alignItems: "center",
  padding: "10px 0",
  borderBottom: "1px solid #0f3460",
};

const memberInfoStyle: React.CSSProperties = {
  flex: 1,
};

export function EquipoDetalle() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [team, setTeam] = useState<TeamDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  useEffect(() => {
    async function loadTeam() {
      if (!id) return;
      setLoading(true);
      setError(null);
      try {
        const data = await getTeamById(id);
        setTeam(data);
      } catch (err) {
        if (err instanceof ApiError) {
          setError(`Error ${err.status}: No se pudo cargar el equipo.`);
        } else {
          setError("Error de conexión. Verificá tu conexión a internet.");
        }
      } finally {
        setLoading(false);
      }
    }
    loadTeam();
  }, [id]);

  async function handleRemoveMember(memberId: string) {
    if (!id || !team) return;
    setActionError(null);
    setActionSuccess(null);

    const confirmed = window.confirm("¿Estás seguro de que querés quitar a este miembro del equipo?");
    if (!confirmed) return;

    try {
      await removeMember(id, memberId);
      setActionSuccess("Miembro removido correctamente.");
      const updated = await getTeamById(id);
      setTeam(updated);
    } catch (err) {
      if (err instanceof ApiError) {
        setActionError(`Error ${err.status}: No se pudo remover el miembro.`);
      } else {
        setActionError("Error de conexión. Verificá tu conexión a internet.");
      }
    }
  }

  if (loading) {
    return <div style={{ ...containerStyle, color: "#aaa" }}>Cargando...</div>;
  }

  if (error || !team) {
    return (
      <div style={containerStyle}>
        <div style={errorStyle}>{error || "Equipo no encontrado"}</div>
        <button onClick={() => navigate("/admin/equipos")} style={buttonStyle("secondary")}>
          Volver al Listado
        </button>
      </div>
    );
  }

  return (
    <div style={containerStyle}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
        <h2>Detalle del Equipo</h2>
        <button onClick={() => navigate("/admin/equipos")} style={buttonStyle("secondary")}>
          Volver
        </button>
      </div>

      {actionSuccess && <div style={successStyle}>{actionSuccess}</div>}
      {actionError && <div style={errorStyle}>{actionError}</div>}

      <div style={cardStyle}>
        <div>
          <p style={labelStyle}>Nombre</p>
          <p style={valueStyle}>{team.name}</p>
        </div>

        <div>
          <p style={labelStyle}>Descripción</p>
          <p style={valueStyle}>{team.description}</p>
        </div>

        <div style={{ display: "flex", gap: 24 }}>
          <div style={{ flex: 1 }}>
            <p style={labelStyle}>Código de Unión</p>
            <code style={{
              display: "inline-block",
              padding: "6px 12px",
              backgroundColor: "#0f3460",
              borderRadius: 4,
              fontSize: "1.1rem",
              letterSpacing: "2px",
            }}>
              {team.joinCode}
            </code>
          </div>

          <div style={{ flex: 1 }}>
            <p style={labelStyle}>Líder</p>
            <p style={valueStyle}>{team.leaderName || team.leaderId}</p>
          </div>
        </div>

        <div style={{ marginTop: "1rem" }}>
          <p style={{ ...labelStyle, marginBottom: 8 }}>Creado</p>
          <p style={{ color: "white", fontSize: 14 }}>
            {new Date(team.createdAt).toLocaleString("es-AR")}
          </p>
        </div>
      </div>

      <div style={cardStyle}>
        <h3 style={{ marginBottom: "1rem", color: "white" }}>Miembros ({team.members.length})</h3>

        {team.members.length === 0 ? (
          <p style={{ color: "#666" }}>Este equipo no tiene miembros.</p>
        ) : (
          team.members.map((member) => (
            <div key={member.id} style={memberRowStyle}>
              <div style={memberInfoStyle}>
                <p style={{ color: "white", fontWeight: 600 }}>{member.name || member.userId}</p>
                <p style={{ color: "#aaa", fontSize: 13 }}>{member.email || ""}</p>
              </div>
              <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <span style={{
                  padding: "2px 8px",
                  borderRadius: 4,
                  fontSize: 12,
                  backgroundColor: member.role === "Leader" ? "#e94560" : "#0f3460",
                  color: "white",
                }}>
                  {member.role}
                </span>
                {member.userId !== team.leaderId && (
                  <button
                    onClick={() => handleRemoveMember(member.id)}
                    style={buttonStyle("danger")}
                  >
                    Quitar
                  </button>
                )}
              </div>
            </div>
          ))
        )}
      </div>

      <div style={{ display: "flex", gap: 8 }}>
        <a
          href={`/admin/equipos/${id}/editar`}
          style={{ ...buttonStyle("primary"), textDecoration: "none" }}
        >
          Editar Equipo
        </a>
      </div>
    </div>
  );
}