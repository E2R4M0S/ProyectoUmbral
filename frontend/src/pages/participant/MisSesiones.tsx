import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { getMySessions } from "../../services/sessionsApi";
import type { MySessionItem, SessionStatus } from "../../types/session";

const STATUS_LABELS: Record<string, string> = {
  Scheduled: "Programada", Preparing: "En Preparación",
  Active: "Activa", Paused: "Pausada",
  Finished: "Finalizada", Cancelled: "Cancelada",
};

function statusClass(s: string) {
  if (s === "Active")    return "badge badge-success";
  if (s === "Preparing") return "badge badge-warning";
  if (s === "Paused")    return "badge badge-warning";
  if (s === "Finished")  return "badge badge-info";
  if (s === "Cancelled") return "badge badge-error";
  return "badge badge-muted";
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("es-VE", { day: "2-digit", month: "short", year: "numeric" });
}

// Sesiones en vivo (aún jugables) van directo al juego; Finished/Cancelled muestran
// la pantalla de resultados — ambas cosas ya las resuelve el propio GameView según
// el estado que trae del backend, así que siempre navegamos a la misma ruta.
function goToSession(navigate: ReturnType<typeof useNavigate>, session: MySessionItem) {
  try { sessionStorage.setItem(`joined_${session.id}`, "1"); } catch { /* ignore */ }
  navigate(`/juego/${session.id}`);
}

export function MisSesiones() {
  const navigate = useNavigate();
  const [sessions, setSessions] = useState<MySessionItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    getMySessions()
      .then(setSessions)
      .catch(() => setError("No se pudieron cargar tus sesiones."))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Mis Sesiones</h1>
          <p className="page-subtitle">Sesiones en las que has participado.</p>
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <div style={{ textAlign: "center", padding: "3rem" }}>
          <div className="spinner" style={{ margin: "0 auto" }} />
        </div>
      ) : sessions.length === 0 ? (
        <div style={{ textAlign: "center", padding: "4rem", color: "var(--text-muted)" }}>
          <div style={{ fontSize: "2.5rem", marginBottom: "1rem" }}>🎮</div>
          <p>Todavía no te has unido a ninguna sesión.</p>
        </div>
      ) : (
        <div className="session-cards">
          {sessions.map(s => (
            <div
              key={s.id}
              className="session-card"
              style={{ cursor: "pointer" }}
              onClick={() => goToSession(navigate, s)}
            >
              <div className="session-card-top">
                <span className={statusClass(s.status)}>
                  {STATUS_LABELS[s.status as SessionStatus] ?? s.status}
                </span>
                <span className="session-card-stat">🏆 {s.myScore} pts</span>
              </div>

              <div className="session-card-title">{s.name}</div>

              {s.missionTitles.length > 0 && (
                <div className="session-card-mission">
                  <span>{s.missionTitles.join(", ")}</span>
                </div>
              )}

              <div className="session-card-stats">
                <span className="session-card-stat">🕐 Te uniste: {formatDate(s.joinedAt)}</span>
                {s.endedAt && (
                  <span className="session-card-stat">🏁 Finalizó: {formatDate(s.endedAt)}</span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
