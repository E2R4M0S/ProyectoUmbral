import { useState, useEffect, useRef } from "react";
import { useParams, Link, useLocation } from "react-router-dom";
import { getSessionProgress, getSessionById, transitionSession, advanceStage, ApiError } from "../../services/sessionsApi";
import { getMissionById } from "../../services/missionsApi";
import { fetchWithAuth } from "../../services/api";
import type { SessionProgress, ParticipantProgress, SessionStatus, SessionStage } from "../../types/session";
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
  stageBox: { padding: "1rem", backgroundColor: "#1a2f5c", border: "2px solid #e94560", borderRadius: 8, marginBottom: "1rem" },
  stageOrder: { color: "#0f3460", backgroundColor: "white", display: "inline-block", padding: "2px 8px", borderRadius: 12, fontSize: 12, fontWeight: 700, marginRight: 8 },
};

const statusColors: Record<string, string> = { Scheduled: "#6c757d", Preparing: "#ffc107", Active: "#28a745", Paused: "#ffc107", Finished: "#007bff", Cancelled: "#dc3545" };
const statusLabels: Record<string, string> = { Scheduled: "Programada", Preparing: "En Preparacion", Active: "Activa", Paused: "Pausada", Finished: "Finalizada", Cancelled: "Cancelada" };

export function PanelSesion() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const basePath = location.pathname.includes("/admin/") ? "/admin" : "/operator";
  const [progress, setProgress] = useState<SessionProgress | null>(null);
  const [stages, setStages] = useState<SessionStage[]>([]);
  const [currentStageOrder, setCurrentStageOrder] = useState(0);
  const [mission, setMission] = useState<MissionDetail | null>(null);
  const [selectedClueId, setSelectedClueId] = useState<string>("");
  const [releasing, setReleasing] = useState(false);
  const [clueMsg, setClueMsg] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [localSeconds, setLocalSeconds] = useState(0);
  const lastServerRef = useRef(0);
  const [selectedQuizId, setSelectedQuizId] = useState<string>("");
  const [advancing, setAdvancing] = useState(false);
  const [advanceMsg, setAdvanceMsg] = useState("");

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

  // Fetch session detail + stages
  useEffect(() => {
    if (!id) return;
    getSessionById(id)
      .then((detail) => {
        setStages(detail.stages ?? []);
        setCurrentStageOrder(detail.currentStageOrder ?? 0);
        const current = (detail.stages ?? []).find(st => st.order === (detail.currentStageOrder ?? 0) + 1);
        if (current) return getMissionById(current.missionId);
        return null;
      })
      .then((missionDetail) => {
        if (!missionDetail) return;
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
    if (!progress || progress.status !== "Active") return;
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

  const handleAdvanceStage = async () => {
    if (!id) return;
    setAdvancing(true);
    setAdvanceMsg("");
    try {
      const result = await advanceStage(id);
      setCurrentStageOrder(result.currentStageOrder);
      setAdvanceMsg(result.isLastStage ? "Última etapa alcanzada." : `Avanzaste a la etapa ${result.currentStageOrder + 1} de ${result.totalStages}.`);
      // Re-fetch mission for the new current stage
      const detail = await getSessionById(id);
      setStages(detail.stages ?? []);
      const current = (detail.stages ?? []).find(st => st.order === result.currentStageOrder + 1);
      if (current) {
        const m = await getMissionById(current.missionId).catch(() => null);
        if (m) {
          setMission(m);
          setSelectedClueId("");
        }
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setAdvanceMsg(err.body || "No se pudo avanzar de etapa.");
      } else {
        setAdvanceMsg("Error al avanzar de etapa.");
      }
    } finally {
      setAdvancing(false);
    }
  };

  if (loading) return <div style={s.container}>Cargando...</div>;
  if (error || !progress) return <div style={s.container}><p style={{ color: "#e94560" }}>{error || "No encontrada"}</p><Link to={`${basePath}/sesiones`} style={s.backLink}>Volver</Link></div>;

  const currentStage = stages.find(st => st.order === currentStageOrder + 1);
  const isLastStage = stages.length === 0 || currentStageOrder >= stages.length - 1;
  const canAdvance = progress.status === "Active" && !isLastStage;

  return (
    <div style={s.container}>
      <Link to={`${basePath}/sesiones`} style={s.backLink}>Volver al listado</Link>
      <div style={{ marginTop: "1rem" }}>
        <h2 style={s.title}>{progress.name}</h2>
        <p style={s.meta}>Estado: <span style={s.badge(statusColors[progress.status] || "#6c757d")}>{statusLabels[progress.status] || progress.status}</span></p>
      </div>

      <div style={s.timerBox}>
        <div style={s.timer}>{formatTime(localSeconds)}</div>
        <div style={{ color: "#999", fontSize: "0.8rem", marginTop: 4 }}>transcurrido</div>
      </div>

      {/* Current Stage Indicator (multi-mission) */}
      {stages.length > 0 && (
        <div style={s.stageBox} data-testid="current-stage-box">
          <div style={{ fontSize: "0.8rem", color: "#999" }}>Etapa actual</div>
          <div style={{ marginTop: 4, fontSize: "1.1rem", fontWeight: 600 }}>
            <span style={s.stageOrder}>{currentStageOrder + 1}/{stages.length}</span>
            {currentStage?.missionTitle ?? "—"}
            <span style={{ color: "#999", marginLeft: 8, fontSize: "0.85rem" }}>
              ({currentStage?.missionType})
            </span>
          </div>
          <div style={{ display: "flex", gap: 8, marginTop: 10 }}>
            <button
              type="button"
              data-testid="advance-stage-btn"
              onClick={handleAdvanceStage}
              disabled={!canAdvance || advancing}
              style={s.btn(canAdvance && !advancing ? "#28a745" : "#555")}
            >
              {advancing ? "Avanzando..." : "Siguiente Etapa →"}
            </button>
            {isLastStage && (
              <span style={{ color: "#ffc107", fontSize: "0.85rem", alignSelf: "center" }}>
                Última etapa
              </span>
            )}
          </div>
          {advanceMsg && (
            <p style={{ marginTop: 6, color: advanceMsg.includes("Error") || advanceMsg.includes("No se pudo") ? "#e94560" : "#28a745", fontSize: "0.85rem" }}>
              {advanceMsg}
            </p>
          )}
        </div>
      )}

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

      {/* Clue selector + release button (solo para misiones Treasure) */}
      {currentStage?.missionType !== "Trivia" && (
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
          )}
        </div>
      )}

        {/* Seleccionar Quiz (Preparing) */}
        {progress.status === "Preparing" && (
          <div style={{ marginTop: "1.5rem" }}>
            <h3 style={s.section}>Seleccionar Quiz</h3>
            <QuizSelector sessionId={id!} selectedQuizId={selectedQuizId} onSelect={setSelectedQuizId} />
          </div>
        )}
        {progress.status === "Active" && selectedQuizId && (
          <div style={{ marginTop: "1.5rem" }}>
            <h3 style={s.section}>Enviar Pregunta de Trivia</h3>
            <QuizQuestionSender sessionId={id!} quizId={selectedQuizId} totalParticipants={progress.participants?.length || 0} />
          </div>
        )}
      </div>
    );
  }

function QuizSelector({ sessionId, selectedQuizId, onSelect }: { sessionId: string; selectedQuizId: string; onSelect: (id: string) => void }) {
  const [quizzes, setQuizzes] = useState<{ id: string; title: string; questionCount: number }[]>([]);

  useEffect(() => {
    fetchWithAuth("/api/quizzes").then(r => r.json()).then(setQuizzes).catch(() => {});
  }, []);

  const selectStyle: React.CSSProperties = {
    width: "100%", padding: "8px", borderRadius: 4, border: "1px solid #0f3460",
    backgroundColor: "#16213e", color: "white", fontSize: "0.9rem", marginBottom: "0.75rem",
  };

  return (
    <div>
      <select value={selectedQuizId} onChange={e => onSelect(e.target.value)} style={selectStyle}>
        <option value="">Seleccionar quiz para esta sesión...</option>
        {quizzes.map(q => (
          <option key={q.id} value={q.id}>{q.title} ({q.questionCount} preg.)</option>
        ))}
      </select>
      {selectedQuizId && <p style={{ color: "#28a745", fontSize: "0.85rem" }}>✓ Quiz seleccionado</p>}
    </div>
  );
}

function QuizQuestionSender({ sessionId, quizId, totalParticipants }: { sessionId: string; quizId: string; totalParticipants: number }) {
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [questions, setQuestions] = useState<{ id: string; text: string; answers: { id: string; text: string; isCorrect: boolean }[] }[]>([]);
  const [sending, setSending] = useState(false);
  const [msg, setMsg] = useState("");
  const [currentQuestionId, setCurrentQuestionId] = useState<string | null>(null);
  const [answerCount, setAnswerCount] = useState(0);

  useEffect(() => {
    if (!quizId) return;
    setCurrentQuestionIndex(0);
    setCurrentQuestionId(null);
    setAnswerCount(0);
    setMsg("");
    fetchWithAuth(`/api/quizzes/${quizId}`)
      .then(r => r.json())
      .then(data => setQuestions(data.questions || []))
      .catch(() => setMsg("Error al cargar preguntas"));
  }, [quizId]);

  // Poll answer count when a question is active
  useEffect(() => {
    if (!currentQuestionId) return;
    const interval = setInterval(async () => {
      try {
        const resp = await fetchWithAuth(`/api/trivia/questions/${currentQuestionId}/answer-count`);
        if (resp.ok) {
          const data = await resp.json();
          setAnswerCount(data.answerCount);
        }
      } catch { }
    }, 2000);
    return () => clearInterval(interval);
  }, [currentQuestionId]);

  const sendCurrentQuestion = async () => {
    if (questions.length === 0) return;
    const q = questions[currentQuestionIndex];
    setSending(true);
    setCurrentQuestionId(null);
    setAnswerCount(0);
    setMsg("");
    try {
      const resp = await fetchWithAuth("/api/trivia/questions/ask", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionId,
          questionText: q.text,
          options: q.answers.map(a => a.text),
          timeLimitSeconds: 30,
          correctAnswerIndex: q.answers.findIndex(a => a.isCorrect),
        }),
      });
      if (!resp.ok) throw new Error(await resp.text());
      const data = await resp.json();
      setCurrentQuestionId(data.questionId);
      setMsg(`Pregunta ${currentQuestionIndex + 1}/${questions.length} enviada`);
    } catch (ex: any) {
      setMsg("Error: " + (ex?.message || "desconocido"));
    } finally {
      setSending(false);
    }
  };

  const advanceToNext = () => {
    if (currentQuestionIndex < questions.length - 1) {
      setCurrentQuestionIndex(i => i + 1);
      setCurrentQuestionId(null);
      setAnswerCount(0);
      setMsg("");
    } else {
      setMsg("¡Todas las preguntas enviadas!");
    }
  };

  const btnStyle = (disabled: boolean): React.CSSProperties => ({
    padding: "8px 16px", backgroundColor: disabled ? "#999" : "#e94560", color: "white",
    border: "none", borderRadius: 6, cursor: disabled ? "not-allowed" : "pointer",
    fontSize: 14, fontWeight: 600,
  });

  return (
    <div>
      {questions.length > 0 && (
        <div style={{ backgroundColor: "#16213e", borderRadius: 8, padding: "1rem", marginTop: "0.5rem", border: "1px solid #0f3460" }}>
          <div style={{ color: "#e94560", fontWeight: 600, marginBottom: "0.5rem" }}>
            Pregunta {currentQuestionIndex + 1} de {questions.length}
          </div>
          <div style={{ color: "white", marginBottom: "0.75rem" }}>{questions[currentQuestionIndex]?.text}</div>
          <div style={{ color: "#999", fontSize: "0.85rem", marginBottom: "0.75rem" }}>
            Opciones: {questions[currentQuestionIndex]?.answers.map(a => a.text).join(", ")}
          </div>

          {currentQuestionId && totalParticipants > 0 && (
            <div style={{ color: "#ccc", fontSize: "0.85rem", marginBottom: "0.5rem" }}>
              Respondieron: {answerCount} / {totalParticipants}
              {answerCount >= totalParticipants && " ✅"}
            </div>
          )}

          {!currentQuestionId ? (
            <button onClick={sendCurrentQuestion} disabled={sending} style={btnStyle(sending)}>
              {sending ? "Enviando..." : "Enviar Pregunta"}
            </button>
          ) : currentQuestionIndex < questions.length - 1 ? (
            <button onClick={advanceToNext} disabled={answerCount < totalParticipants}
              style={btnStyle(answerCount < totalParticipants)}>
              {answerCount >= totalParticipants ? "Siguiente →" : "Esperando respuestas..."}
            </button>
          ) : null}
          {msg && (
            <p style={{ marginTop: "0.5rem", color: msg.includes("Error") ? "#e94560" : "#28a745", fontSize: "0.85rem" }}>{msg}</p>
          )}
        </div>
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
