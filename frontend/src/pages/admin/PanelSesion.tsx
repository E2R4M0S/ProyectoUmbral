import { useState, useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import { getSessionProgress, transitionSession, ApiError } from "../../services/sessionsApi";
import { fetchWithAuth } from "../../services/api";
import type { SessionProgress, ParticipantProgress, SessionStatus } from "../../types/session";

const s: Record<string, React.CSSProperties> = {
  container: { maxWidth: 700, margin: "0 auto", color: "white", fontFamily: "sans-serif", padding: "1rem" },
  title: { fontSize: "1.5rem", margin: 0, color: "#e94560" },
  meta: { color: "#999", fontSize: "0.9rem", marginTop: "0.25rem" },
  section: { fontSize: "1.1rem", color: "#e94560", margin: "1.5rem 0 0.5rem" },
  card: { padding: "1rem", backgroundColor: "#16213e", borderRadius: 8, border: "1px solid #0f3460", marginBottom: "0.75rem" },
  badge: (bg: string) => ({ display: "inline-block", padding: "2px 8px", borderRadius: 12, fontSize: 12, fontWeight: 600, color: "white", backgroundColor: bg }),
  btn: (bg: string) => ({ padding: "6px 14px", backgroundColor: bg, color: "white", border: "none", borderRadius: 4, cursor: "pointer", fontSize: "0.85rem", fontWeight: 600 }),
  backLink: { color: "#e94560", textDecoration: "none", fontSize: "0.9rem" },
  timerBox: { textAlign: "center" as const, padding: "1rem", backgroundColor: "#16213e", borderRadius: 8, marginBottom: "1rem" },
  timer: { fontSize: "3rem", fontWeight: 700, color: "#e94560", fontFamily: "monospace" },
};

const statusColors: Record<string, string> = { Scheduled: "#6c757d", Preparing: "#ffc107", Active: "#28a745", Paused: "#ffc107", Finished: "#007bff", Cancelled: "#dc3545" };
const statusLabels: Record<string, string> = { Scheduled: "Programada", Preparing: "En Preparacion", Active: "Activa", Paused: "Pausada", Finished: "Finalizada", Cancelled: "Cancelada" };

export function PanelSesion() {
  const { id } = useParams<{ id: string }>();
  const [progress, setProgress] = useState<SessionProgress | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function load() {
    if (!id) return;
    setLoading(true);
    try { setProgress(await getSessionProgress(id)); setError(""); } catch { setError("No se pudo cargar."); }
    finally { setLoading(false); }
  }
  useEffect(() => { load(); const i = setInterval(load, 5000); return () => clearInterval(i); }, [id]);

  if (loading) return <div style={s.container}>Cargando...</div>;
  if (error || !progress) return <div style={s.container}><p style={{ color: "#e94560" }}>{error || "No encontrada"}</p><Link to="/operator/sesiones" style={s.backLink}>Volver</Link></div>;

  return (
    <div style={s.container}>
      <Link to="/operator/sesiones" style={s.backLink}>Volver al listado</Link>
      <div style={{ marginTop: "1rem" }}>
        <h2 style={s.title}>{progress.name}</h2>
        <p style={s.meta}>Estado: <span style={s.badge(statusColors[progress.status] || "#6c757d")}>{statusLabels[progress.status] || progress.status}</span></p>
      </div>

      <div style={s.timerBox}>
        <div style={s.timer}>{formatTime(progress.elapsedSeconds)}</div>
        <div style={{ color: "#999", fontSize: "0.8rem", marginTop: 4 }}>transcurrido</div>
      </div>

      {getTransitions(progress.status as SessionStatus).length > 0 && (
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: "1rem" }}>
          {getTransitions(progress.status as SessionStatus).map(t => (
            <button key={t} onClick={async () => {
              try { await transitionSession(id!, t); load(); } catch {}
            }} style={{
              padding: "8px 16px", border: "none", borderRadius: 6, cursor: "pointer",
              fontSize: 14, fontWeight: 600, color: "white",
              backgroundColor: t === "Cancelled" || t === "Finished" ? "#dc3545" : t === "Active" ? "#28a745" : "#0f3460",
            }}>
              {t === "Preparing" ? "Preparar" : t === "Active" ? "Iniciar" : t === "Paused" ? "Pausar" : t === "Finished" ? "Finalizar" : t}
            </button>
          ))}
        </div>
      )}

      <h3 style={s.section}>Participantes ({progress.participants?.length || 0})</h3>
      {progress.participants?.length ? (
        progress.participants.map((p: ParticipantProgress, i: number) => (
          <div key={i} style={s.card}>
            <span style={{ fontWeight: 600 }}>{p.userAlias}</span>
            <span style={{ color: "#999", marginLeft: 8, fontSize: "0.85rem" }}>
              Unido: {new Date(p.joinedAt).toLocaleTimeString("es-AR")}
            </span>
          </div>
        ))
      ) : <p style={{ color: "#999" }}>Sin participantes</p>}

      <div style={{ marginTop: "1.5rem" }}>
        <button style={s.btn("#e94560")} onClick={async () => {
          try {
            const resp = await fetchWithAuth(`/api/sessions/${id}/clues/release`, {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({ teamId: null }),
            });
            if (!resp.ok) throw new Error("Error");
            alert("Pista liberada");
          } catch { alert("Error al liberar pista"); }
        }}>
          Liberar Pista
        </button>
      </div>
    </div>
  );
}

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60); const sec = seconds % 60;
  return `${m.toString().padStart(2, "0")}:${sec.toString().padStart(2, "0")}`;
}

function getTransitions(status: SessionStatus): string[] {
  switch (status) { case "Scheduled": return ["Preparing", "Cancelled"]; case "Preparing": return ["Active", "Cancelled"]; case "Active": return ["Paused", "Finished", "Cancelled"]; case "Paused": return ["Active", "Finished", "Cancelled"]; default: return []; }
}
