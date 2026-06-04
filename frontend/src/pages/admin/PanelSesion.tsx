import { useState, useEffect, useRef } from "react";
import { useParams, Link } from "react-router-dom";
import { getSessionProgress, getSessionById, transitionSession } from "../../services/sessionsApi";
import { getMissionById } from "../../services/missionsApi";
import { fetchWithAuth } from "../../services/api";
import type { SessionProgress, ParticipantProgress, SessionStatus } from "../../types/session";
import type { MissionDetail, Clue } from "../../types/mission";

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
  const [mission, setMission] = useState<MissionDetail | null>(null);
  const [selectedClueId, setSelectedClueId] = useState<string>("");
  const [releasing, setReleasing] = useState(false);
  const [clueMsg, setClueMsg] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [localSeconds, setLocalSeconds] = useState(0);
  const lastServerRef = useRef(0);

  async function load() {
    if (!id) return;
    try {
      const p = await getSessionProgress(id);
      setProgress(p);
      lastServerRef.current = p.elapsedSeconds;
      setLocalSeconds(p.elapsedSeconds);
      setError("");
    } catch {
      setError("No se pudo cargar.");
    } finally {
      setLoading(false);
    }
  }

  // Fetch session detail + mission on mount to get clues
  useEffect(() => {
    if (!id) return;
    getSessionById(id)
      .then((detail) => getMissionById(detail.missionId))
      .then((missionDetail) => {
        setMission(missionDetail);
        // Auto-select first clue
        const firstClue = missionDetail.stages?.flatMap(st => st.clues ?? [])?.[0];
        if (firstClue) setSelectedClueId(firstClue.id);
      })
      .catch(() => { /* mission fetch is best-effort */ });
  }, [id]);

  useEffect(() => { load(); const i = setInterval(load, 5000); return () => clearInterval(i); }, [id]);

  // Local 1-second tick for smooth timer display
  useEffect(() => {
    if (!progress || (progress.status !== "Active" && progress.status !== "Paused")) return;
    const tick = setInterval(() => {
      setLocalSeconds(prev => prev + 1);
    }, 1000);
    return () => clearInterval(tick);
  }, [progress?.status]);

  // Sync local timer to server value when it updates
  useEffect(() => {
    if (progress) {
      setLocalSeconds(progress.elapsedSeconds);
    }
  }, [progress?.elapsedSeconds]);

  // Flatten all clues from all stages
  const allClues: Clue[] = mission?.stages?.flatMap(st => st.clues ?? []) ?? [];
  const selectedClue = allClues.find(c => c.id === selectedClueId);

  const handleReleaseClue = async () => {
    if (!selectedClueId) return;
    setReleasing(true);
    setClueMsg("");
    try {
      const resp = await fetchWithAuth(`/api/sessions/${id}/clues/release`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ clueId: selectedClueId, teamId: null }),
      });
      if (!resp.ok) throw new Error("Error");
      const result = await resp.json();
      setClueMsg(result.hasContent ? "Pista liberada con contenido real" : "Pista liberada (sin contenido en la mision)");
    } catch {
      setClueMsg("Error al liberar pista");
    } finally {
      setReleasing(false);
    }
  };

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
        <div style={s.timer}>{formatTime(localSeconds)}</div>
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

      {/* Clue selector + release button */}
      <div style={{ marginTop: "1.5rem" }}>
        <h3 style={s.section}>Pistas de la mision</h3>
        {allClues.length === 0 ? (
          <p style={{ color: "#999" }}>Esta mision no tiene pistas definidas.</p>
        ) : (
          <>
            <select
              value={selectedClueId}
              onChange={e => setSelectedClueId(e.target.value)}
              style={{
                width: "100%", padding: "8px", borderRadius: 4, border: "1px solid #0f3460",
                backgroundColor: "#16213e", color: "white", fontSize: "0.9rem", marginBottom: "0.75rem",
              }}
            >
              {allClues.map(clue => (
                <option key={clue.id} value={clue.id}>
                  {clue.content?.substring(0, 80)}{clue.content?.length > 80 ? "..." : ""}
                  {clue.penalty != null ? ` (penalizacion: ${clue.penalty}pts)` : ""}
                </option>
              ))}
            </select>
            <button
              style={s.btn("#e94560")}
              onClick={handleReleaseClue}
              disabled={releasing || !selectedClueId}
            >
              {releasing ? "Liberando..." : "Liberar Pista"}
            </button>
            {clueMsg && (
              <p style={{ marginTop: "0.5rem", color: clueMsg.includes("Error") ? "#e94560" : "#28a745", fontSize: "0.85rem" }}>
                {clueMsg}
              </p>
            )}
            {selectedClue && (
              <div style={{ ...s.card, marginTop: "0.75rem" }}>
                <div style={{ fontWeight: 600, marginBottom: 4 }}>Vista previa:</div>
                <div style={{ color: "#ccc", fontSize: "0.9rem" }}>{selectedClue.content}</div>
                {selectedClue.penalty != null && (
                  <div style={{ color: "#e94560", fontSize: "0.8rem", marginTop: 4 }}>
                    Penalizacion: {selectedClue.penalty} puntos
                  </div>
                )}
              </div>
            )}
          </>
        {/* Trivia: Send Question */}
      {progress.status === "Active" && (
        <div style={{ marginTop: "1.5rem" }}>
          <h3 style={s.section}>Enviar Pregunta de Trivia</h3>
          <TriviaQuestionSender sessionId={id!} />
        </div>
      )}
    </div>
  );
}

function TriviaQuestionSender({ sessionId }: { sessionId: string }) {
  const [questionText, setQuestionText] = useState("");
  const [options, setOptions] = useState(["", "", "", ""]);
  const [sending, setSending] = useState(false);
  const [msg, setMsg] = useState("");

  const sendQuestion = async () => {
    if (!questionText.trim() || options.some(o => !o.trim())) {
      setMsg("Completá la pregunta y todas las opciones");
      return;
    }
    setSending(true);
    setMsg("");
    try {
      const resp = await fetchWithAuth(`/api/trivia/questions/ask`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionId,
          questionText: questionText.trim(),
          options: options.map(o => o.trim()),
          timeLimitSeconds: 30,
        }),
      });
      if (!resp.ok) throw new Error(await resp.text());
      setMsg("¡Pregunta enviada!");
      setQuestionText("");
      setOptions(["", "", "", ""]);
    } catch (ex: any) {
      setMsg("Error: " + (ex?.message || "desconocido"));
    } finally {
      setSending(false);
    }
  };

  const inputStyle: React.CSSProperties = {
    width: "100%", padding: "8px", borderRadius: 4, border: "1px solid #0f3460",
    backgroundColor: "#16213e", color: "white", fontSize: "0.9rem", marginBottom: "0.5rem", boxSizing: "border-box",
  };

  return (
    <div>
      <textarea
        placeholder="Escribí la pregunta..."
        value={questionText}
        onChange={e => setQuestionText(e.target.value)}
        rows={2}
        style={inputStyle}
      />
      {options.map((opt, i) => (
        <input
          key={i}
          placeholder={`Opción ${i + 1}`}
          value={opt}
          onChange={e => {
            const next = [...options];
            next[i] = e.target.value;
            setOptions(next);
          }}
          style={inputStyle}
        />
      ))}
      <button
        onClick={sendQuestion}
        disabled={sending}
        style={{
          padding: "8px 16px", backgroundColor: "#e94560", color: "white",
          border: "none", borderRadius: 6, cursor: sending ? "not-allowed" : "pointer",
          fontSize: 14, fontWeight: 600,
        }}
      >
        {sending ? "Enviando..." : "Enviar Pregunta"}
      </button>
      {msg && (
        <p style={{ marginTop: "0.5rem", color: msg.includes("Error") ? "#e94560" : "#28a745", fontSize: "0.85rem" }}>
          {msg}
        </p>
      )}
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
