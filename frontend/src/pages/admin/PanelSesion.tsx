import { useState, useEffect, useRef, useCallback } from "react";
import { useParams, Link, useLocation } from "react-router-dom";
import QRCode from "qrcode";
import { getSessionProgress, getSessionById, transitionSession, advanceStage, ApiError } from "../../services/sessionsApi";
import { getMissionById } from "../../services/missionsApi";
import { fetchWithAuth } from "../../services/api";
import { useSignalR } from "../../hooks/useSignalR";
import type { AnswerResult } from "../../hooks/useSignalR";
import type { SessionProgress, ParticipantProgress, SessionStatus, SessionStage } from "../../types/session";
import type { MissionDetail, Clue } from "../../types/mission";
import type { RankingEntry } from "../../types/game";

// ── Styles ────────────────────────────────────────────────────────────────────

const css = {
  container: { maxWidth: 800, margin: "0 auto", color: "white", padding: "0 0 3rem" } as React.CSSProperties,
  backLink: { color: "#e94560", textDecoration: "none", fontSize: "0.875rem", display: "inline-flex", alignItems: "center", gap: 4, marginBottom: "1.25rem" } as React.CSSProperties,

  // Header
  headerCard: { backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 10, padding: "1.25rem 1.5rem", marginBottom: "1.25rem" } as React.CSSProperties,
  sessionName: { fontSize: "1.4rem", fontWeight: 700, color: "#e94560", margin: "0 0 0.5rem" } as React.CSSProperties,
  headerMeta: { display: "flex", flexWrap: "wrap" as const, gap: "0.75rem", alignItems: "center" } as React.CSSProperties,
  chip: (bg: string): React.CSSProperties => ({ padding: "4px 12px", borderRadius: 20, fontSize: 12, fontWeight: 700, backgroundColor: bg, color: "white" }),

  // PIN
  pinBox: { backgroundColor: "#0d1b35", border: "1px solid #0f3460", borderRadius: 8, padding: "0.75rem 1.25rem", display: "flex", alignItems: "center", gap: "0.75rem", marginBottom: "1.25rem" } as React.CSSProperties,
  pinLabel: { color: "#888", fontSize: "0.78rem", fontWeight: 700, textTransform: "uppercase" as const, letterSpacing: 1 } as React.CSSProperties,
  pinCode: { fontFamily: "monospace", fontSize: "1.75rem", fontWeight: 700, letterSpacing: "0.25em", color: "white" } as React.CSSProperties,

  // Timer
  timerBox: { display: "flex", flexDirection: "column" as const, alignItems: "center", backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 10, padding: "1rem 2rem", marginBottom: "1.25rem" } as React.CSSProperties,
  timerValue: { fontSize: "3rem", fontWeight: 700, color: "#e94560", fontFamily: "monospace", lineHeight: 1 } as React.CSSProperties,
  timerLabel: { color: "#666", fontSize: "0.75rem", textTransform: "uppercase" as const, letterSpacing: 1, marginTop: 4 } as React.CSSProperties,

  // Sections
  sectionTitle: { fontSize: "0.8rem", fontWeight: 700, color: "#e94560", textTransform: "uppercase" as const, letterSpacing: 1, margin: "1.5rem 0 0.75rem" } as React.CSSProperties,

  // Stage box
  stageCard: (type: string): React.CSSProperties => ({
    backgroundColor: "#16213e",
    border: `1px solid ${type === "Treasure" ? "#e94560" : "#0f3460"}`,
    borderLeft: `4px solid ${type === "Treasure" ? "#e94560" : "#3b82f6"}`,
    borderRadius: 8,
    padding: "1rem 1.25rem",
    marginBottom: "1rem",
  }),
  stageTypeChip: (type: string): React.CSSProperties => ({
    display: "inline-flex", alignItems: "center", gap: 4,
    padding: "3px 10px", borderRadius: 20, fontSize: 12, fontWeight: 700,
    backgroundColor: type === "Treasure" ? "#3d1a1a" : "#1a2d4a",
    color: type === "Treasure" ? "#e94560" : "#3b82f6",
    border: `1px solid ${type === "Treasure" ? "#e94560" : "#3b82f6"}`,
  }),

  // Buttons
  btnPrimary: { padding: "9px 20px", backgroundColor: "#e94560", color: "white", border: "none", borderRadius: 7, cursor: "pointer", fontSize: "0.875rem", fontWeight: 700 } as React.CSSProperties,
  btnGreen: { padding: "9px 20px", backgroundColor: "#1a4d2e", color: "#4caf50", border: "1px solid #4caf50", borderRadius: 7, cursor: "pointer", fontSize: "0.875rem", fontWeight: 700 } as React.CSSProperties,
  btnBlue: { padding: "9px 20px", backgroundColor: "#0f3460", color: "white", border: "none", borderRadius: 7, cursor: "pointer", fontSize: "0.875rem", fontWeight: 700 } as React.CSSProperties,
  btnGhost: { padding: "9px 20px", backgroundColor: "transparent", color: "#888", border: "1px solid #444", borderRadius: 7, cursor: "pointer", fontSize: "0.875rem", fontWeight: 700 } as React.CSSProperties,
  btnDisabled: { padding: "9px 20px", backgroundColor: "#2a2a2a", color: "#555", border: "1px solid #333", borderRadius: 7, cursor: "not-allowed", fontSize: "0.875rem", fontWeight: 700 } as React.CSSProperties,

  // Participant card
  participantCard: { display: "flex", alignItems: "center", justifyContent: "space-between", padding: "0.6rem 1rem", backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 7, marginBottom: "0.4rem" } as React.CSSProperties,

  // Notification messages
  msgSuccess: { padding: "8px 14px", backgroundColor: "#1a3d1a", border: "1px solid #4caf50", borderRadius: 6, color: "#4caf50", fontSize: "0.82rem", marginTop: "0.5rem" } as React.CSSProperties,
  msgError: { padding: "8px 14px", backgroundColor: "#2d1a1a", border: "1px solid #e94560", borderRadius: 6, color: "#e94560", fontSize: "0.82rem", marginTop: "0.5rem" } as React.CSSProperties,

  // Select
  select: { width: "100%", padding: "8px 10px", borderRadius: 6, border: "1px solid #0f3460", backgroundColor: "#16213e", color: "white", fontSize: "0.875rem", marginBottom: "0.75rem" } as React.CSSProperties,
};

const statusColors: Record<string, string> = {
  Scheduled: "#6c757d", Preparing: "#fd7e14",
  Active: "#28a745", Paused: "#ffc107",
  Finished: "#3b82f6", Cancelled: "#dc3545",
};
const statusLabels: Record<string, string> = {
  Scheduled: "Programada", Preparing: "En Preparación",
  Active: "Activa", Paused: "Pausada",
  Finished: "Finalizada", Cancelled: "Cancelada",
};

// ── Component ─────────────────────────────────────────────────────────────────

export function PanelSesion() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const basePath = location.pathname.includes("/admin/") ? "/admin" : "/operator";

  const [progress, setProgress] = useState<SessionProgress | null>(null);
  const [pin, setPin] = useState<string>("");
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
  const [ranking, setRanking] = useState<RankingEntry[]>([]);
  const [questionResults, setQuestionResults] = useState<AnswerResult[]>([]);

  async function load() {
    if (!id) return;
    try {
      const p = await getSessionProgress(id);
      setProgress(p);
      lastServerRef.current = p.elapsedSeconds;
      setLocalSeconds(p.elapsedSeconds);
      setError("");
    } catch {
      setError("No se pudo cargar la sesión.");
    } finally {
      setLoading(false);
    }
  }

  async function loadMissionForStage(stageList: SessionStage[], stageOrder: number) {
    const current = stageList.find(st => st.order === stageOrder + 1);
    if (!current) return;
    try {
      const m = await getMissionById(current.missionId);
      setMission(m);
      const firstClue = m.stages?.flatMap(st => st.clues ?? [])?.[0];
      if (firstClue) setSelectedClueId(firstClue.id);
    } catch { /* best-effort */ }
  }

  useEffect(() => {
    if (!id) return;
    getSessionById(id)
      .then((detail) => {
        const stageList = detail.stages ?? [];
        const currentOrder = detail.currentStageOrder ?? 0;
        setStages(stageList);
        setCurrentStageOrder(currentOrder);
        setPin(detail.pin ?? "");
        return loadMissionForStage(stageList, currentOrder);
      })
      .catch(() => {});
  }, [id]);

  useEffect(() => { load(); const i = setInterval(load, 5000); return () => clearInterval(i); }, [id]);

  useEffect(() => {
    if (!progress || progress.status !== "Active") return;
    const tick = setInterval(() => setLocalSeconds(prev => prev + 1), 1000);
    return () => clearInterval(tick);
  }, [progress?.status]);

  useEffect(() => {
    if (progress) setLocalSeconds(progress.elapsedSeconds);
  }, [progress?.elapsedSeconds]);

  useSignalR({
    sessionId: id ?? "",
    onStatusChanged: () => { load(); },
    onProgressUpdated: () => {},
    onClueReleased: () => {},
    onConnectionStateChange: () => {},
    onRankingUpdated: (incoming) => { setRanking(incoming); },
    onQuestionResultsUpdated: (_, results) => { setQuestionResults(results); },
  });

  const allClues: Clue[] = mission?.stages?.flatMap(st => st.clues ?? []) ?? [];
  const selectedClue = allClues.find(c => c.id === selectedClueId);
  const currentStage = stages.find(st => st.order === currentStageOrder + 1);
  const isLastStage = stages.length === 0 || currentStageOrder >= stages.length - 1;
  const canAdvance = progress?.status === "Active" && !isLastStage;
  const isTreasure = currentStage?.missionType === "Treasure";
  const isTerminal = progress?.status === "Finished" || progress?.status === "Cancelled";

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
      if (!resp.ok) throw new Error();
      const result = await resp.json();
      setClueMsg(result.hasContent ? "Pista enviada correctamente." : "Pista enviada (sin contenido definido).");
    } catch {
      setClueMsg("Error al liberar la pista.");
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
      setAdvanceMsg(result.isLastStage ? "✓ Última etapa alcanzada." : `✓ Avanzaste a la etapa ${result.currentStageOrder + 1} de ${result.totalStages}.`);
      const detail = await getSessionById(id);
      setStages(detail.stages ?? []);
      await loadMissionForStage(detail.stages ?? [], result.currentStageOrder);
    } catch (err) {
      setAdvanceMsg(err instanceof ApiError ? (err.body || "No se pudo avanzar.") : "Error al avanzar.");
    } finally {
      setAdvancing(false);
    }
  };

  const handleTransition = async (newStatus: string) => {
    if (!id) return;
    try { await transitionSession(id, newStatus); load(); } catch {}
  };

  if (loading) return <div style={css.container}><p style={{ color: "#aaa" }}>Cargando sesión...</p></div>;
  if (error || !progress) return (
    <div style={css.container}>
      <p style={{ color: "#e94560" }}>{error || "Sesión no encontrada"}</p>
      <Link to={`${basePath}/sesiones`} style={css.backLink}>← Volver</Link>
    </div>
  );

  return (
    <div style={css.container}>
      <Link to={`${basePath}/sesiones`} style={css.backLink}>← Volver al listado</Link>

      {/* Header */}
      <div style={css.headerCard}>
        <h2 style={css.sessionName}>{progress.name}</h2>
        <div style={css.headerMeta}>
          <span style={css.chip(statusColors[progress.status] || "#6c757d")}>
            {statusLabels[progress.status] || progress.status}
          </span>
          {stages.length > 0 && (
            <span style={css.chip("#1a2a3a")}>
              {stages.length} {stages.length === 1 ? "etapa" : "etapas"}
            </span>
          )}
          <span style={css.chip("#1a2a3a")}>
            {progress.participants?.length || 0} participantes
          </span>
        </div>
      </div>

      {/* PIN */}
      <div style={css.pinBox}>
        <div>
          <div style={css.pinLabel}>PIN de la sesión</div>
          <div style={css.pinCode}>{pin || "------"}</div>
        </div>
        <div style={{ marginLeft: "auto", color: "#555", fontSize: "0.78rem" }}>
          Compartí este código<br/>con los participantes
        </div>
      </div>

      {/* Timer */}
      <div style={css.timerBox}>
        <div style={css.timerValue}>{formatTime(localSeconds)}</div>
        <div style={css.timerLabel}>tiempo transcurrido</div>
      </div>

      {/* Transition buttons */}
      {!isTerminal && getTransitions(progress.status as SessionStatus).length > 0 && (
        <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", marginBottom: "1.25rem" }}>
          {getTransitions(progress.status as SessionStatus).map(t => (
            <button key={t} onClick={() => handleTransition(t)} style={
              t === "Cancelled" ? css.btnPrimary
              : t === "Finished" ? { ...css.btnPrimary, backgroundColor: "#dc3545" }
              : t === "Active" ? css.btnGreen
              : css.btnBlue
            }>
              {transitionLabel(t)}
            </button>
          ))}
        </div>
      )}

      {/* Current stage */}
      {stages.length > 0 && (
        <>
          <div style={css.sectionTitle}>Etapa actual</div>
          <div style={css.stageCard(currentStage?.missionType ?? "")}>
            <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: "1rem" }}>
              <div>
                <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", marginBottom: "0.25rem" }}>
                  <span style={{ fontWeight: 700, fontSize: "1rem" }}>
                    {currentStageOrder + 1} / {stages.length}
                  </span>
                  <span style={{ color: "#ccc" }}>{currentStage?.missionTitle ?? "—"}</span>
                </div>
                <span style={css.stageTypeChip(currentStage?.missionType ?? "")}>
                  {currentStage?.missionType === "Treasure" ? "🗺 Búsqueda del Tesoro" : "❓ Trivia"}
                </span>
              </div>
              {!isTerminal && (
                <button
                  onClick={handleAdvanceStage}
                  disabled={!canAdvance || advancing}
                  style={canAdvance && !advancing ? css.btnGreen : css.btnDisabled}
                >
                  {advancing ? "Avanzando..." : isLastStage ? "Última etapa" : "Siguiente →"}
                </button>
              )}
            </div>
            {advanceMsg && (
              <div style={advanceMsg.includes("Error") || advanceMsg.includes("No se pudo") ? css.msgError : css.msgSuccess}>
                {advanceMsg}
              </div>
            )}
          </div>
        </>
      )}

      {/* Participants */}
      <div style={css.sectionTitle}>Participantes ({progress.participants?.length || 0})</div>
      {progress.participants?.length ? (
        progress.participants.map((p: ParticipantProgress, i: number) => (
          <div key={i} style={css.participantCard}>
            <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>{p.userAlias}</span>
            <span style={{ color: "#666", fontSize: "0.78rem" }}>
              Unido: {new Date(p.joinedAt).toLocaleTimeString("es-AR")}
            </span>
          </div>
        ))
      ) : (
        <p style={{ color: "#555", fontSize: "0.875rem" }}>Aún no hay participantes.</p>
      )}

      {/* QR Codes — download section for Treasure stages */}
      {stages.some(st => st.missionType === "Treasure" && st.missionStageId && st.qrToken) && (
        <>
          <div style={css.sectionTitle}>Códigos QR</div>
          <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "1rem" }}>
            <p style={{ color: "#888", fontSize: "0.82rem", margin: "0 0 1rem" }}>
              Descargá los QR para imprimirlos y colocarlos en las ubicaciones físicas.
            </p>
            <div style={{ display: "flex", flexDirection: "column", gap: "0.5rem" }}>
              {stages.filter(st => st.missionType === "Treasure" && st.missionStageId && st.qrToken).map(st => (
                <QrDownloadRow key={st.order} stage={st} />
              ))}
            </div>
          </div>
        </>
      )}

      {/* Clues — only for Treasure */}
      {isTreasure && !isTerminal && (
        <>
          <div style={css.sectionTitle}>Pistas disponibles</div>
          {allClues.length === 0 ? (
            <p style={{ color: "#555", fontSize: "0.875rem" }}>Esta misión no tiene pistas definidas.</p>
          ) : (
            <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "1rem" }}>
              <select value={selectedClueId} onChange={e => setSelectedClueId(e.target.value)} style={css.select}>
                {allClues.map(clue => (
                  <option key={clue.id} value={clue.id}>
                    {clue.content?.substring(0, 80)}{(clue.content?.length ?? 0) > 80 ? "..." : ""}
                    {clue.penalty != null ? ` (−${clue.penalty} pts)` : ""}
                  </option>
                ))}
              </select>
              {selectedClue && (
                <div style={{ padding: "0.75rem", backgroundColor: "#0d1b35", borderRadius: 6, marginBottom: "0.75rem", fontSize: "0.875rem", color: "#ccc", lineHeight: 1.5 }}>
                  {selectedClue.content}
                  {selectedClue.penalty != null && (
                    <span style={{ color: "#e94560", marginLeft: 8, fontSize: "0.78rem" }}>−{selectedClue.penalty} pts</span>
                  )}
                </div>
              )}
              <button onClick={handleReleaseClue} disabled={releasing || !selectedClueId} style={releasing ? css.btnDisabled : css.btnPrimary}>
                {releasing ? "Enviando..." : "Liberar Pista"}
              </button>
              {clueMsg && <div style={clueMsg.includes("Error") ? css.msgError : css.msgSuccess}>{clueMsg}</div>}
            </div>
          )}
        </>
      )}

      {/* Trivia controls */}
      {!isTreasure && !isTerminal && progress.status === "Active" && (
        <>
          <div style={css.sectionTitle}>Quiz de Trivia</div>
          <QuizSelector selectedQuizId={selectedQuizId} onSelect={setSelectedQuizId} />
          {selectedQuizId && (
            <>
              <div style={{ ...css.sectionTitle, marginTop: "1.25rem" }}>Enviar Preguntas</div>
              <QuizQuestionSender sessionId={id!} quizId={selectedQuizId} totalParticipants={progress.participants?.length || 0} questionResults={questionResults} onClearResults={() => setQuestionResults([])} />
            </>
          )}
          {ranking.length > 0 && (
            <>
              <div style={css.sectionTitle}>Ranking en vivo</div>
              <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, overflow: "hidden" }}>
                {ranking.map((entry, i) => (
                  <div key={i} style={{
                    display: "flex", alignItems: "center", justifyContent: "space-between",
                    padding: "0.6rem 1rem",
                    borderBottom: i < ranking.length - 1 ? "1px solid #0f3460" : "none",
                  }}>
                    <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
                      <span style={{ fontWeight: 700, minWidth: 24, color: i === 0 ? "#fbbf24" : i === 1 ? "#9ca3af" : i === 2 ? "#cd7f32" : "#555" }}>
                        #{entry.position}
                      </span>
                      <span style={{ color: "white", fontSize: "0.9rem" }}>{entry.teamName}</span>
                    </div>
                    <span style={{ fontWeight: 700, color: "#e94560" }}>{entry.score} pts</span>
                  </div>
                ))}
              </div>
            </>
          )}
        </>
      )}
    </div>
  );
}

// ── Sub-components ─────────────────────────────────────────────────────────────

function QuizSelector({ selectedQuizId, onSelect }: { selectedQuizId: string; onSelect: (id: string) => void }) {
  const [quizzes, setQuizzes] = useState<{ id: string; title: string; questionCount: number }[]>([]);

  useEffect(() => {
    fetchWithAuth("/api/quizzes").then(r => r.json()).then(setQuizzes).catch(() => {});
  }, []);

  return (
    <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "1rem" }}>
      <select value={selectedQuizId} onChange={e => onSelect(e.target.value)} style={{
        width: "100%", padding: "8px 10px", borderRadius: 6, border: "1px solid #0f3460",
        backgroundColor: "#16213e", color: "white", fontSize: "0.875rem",
      }}>
        <option value="">Seleccionar quiz para esta sesión...</option>
        {quizzes.map(q => (
          <option key={q.id} value={q.id}>{q.title} ({q.questionCount} preg.)</option>
        ))}
      </select>
      {selectedQuizId && <p style={{ color: "#4caf50", fontSize: "0.82rem", margin: "0.5rem 0 0" }}>✓ Quiz seleccionado</p>}
    </div>
  );
}

function QuizQuestionSender({ sessionId, quizId, totalParticipants, questionResults, onClearResults }: { sessionId: string; quizId: string; totalParticipants: number; questionResults: AnswerResult[]; onClearResults: () => void }) {
  const [currentIdx, setCurrentIdx] = useState(0);
  const [questions, setQuestions] = useState<{ id: string; text: string; answers: { id: string; text: string; isCorrect: boolean }[] }[]>([]);
  const [sending, setSending] = useState(false);
  const [closing, setClosing] = useState(false);
  const [questionClosed, setQuestionClosed] = useState(false);
  const [msg, setMsg] = useState("");
  const [currentQuestionId, setCurrentQuestionId] = useState<string | null>(null);
  const [answerCount, setAnswerCount] = useState(0);
  const [cooldown, setCooldown] = useState(0);
  const prevAllAnswered = useRef(false);

  useEffect(() => {
    if (!quizId) return;
    setCurrentIdx(0); setCurrentQuestionId(null); setAnswerCount(0); setMsg(""); setCooldown(0);
    prevAllAnswered.current = false;
    fetchWithAuth(`/api/quizzes/${quizId}`).then(r => r.json())
      .then(data => setQuestions(data.questions || []))
      .catch(() => setMsg("Error al cargar preguntas"));
  }, [quizId]);

  useEffect(() => {
    if (!currentQuestionId) return;
    const interval = setInterval(async () => {
      try {
        const resp = await fetchWithAuth(`/api/trivia/questions/${currentQuestionId}/answer-count`);
        if (resp.ok) { const data = await resp.json(); setAnswerCount(data.answerCount); }
      } catch { }
    }, 2000);
    return () => clearInterval(interval);
  }, [currentQuestionId]);

  const allAnswered = totalParticipants > 0 && answerCount >= totalParticipants;

  // Start 5-second review cooldown when all participants finish answering
  useEffect(() => {
    if (allAnswered && !prevAllAnswered.current) {
      setCooldown(5);
    }
    prevAllAnswered.current = allAnswered;
  }, [allAnswered]);

  useEffect(() => {
    if (cooldown <= 0) return;
    const t = setTimeout(() => setCooldown(c => c - 1), 1000);
    return () => clearTimeout(t);
  }, [cooldown]);

  const canGoNext = allAnswered && cooldown === 0;

  const sendQuestion = async () => {
    if (!questions.length) return;
    const q = questions[currentIdx];
    setSending(true); setCurrentQuestionId(null); setAnswerCount(0); setMsg(""); setCooldown(0);
    setQuestionClosed(false); onClearResults();
    prevAllAnswered.current = false;
    try {
      const resp = await fetchWithAuth("/api/trivia/questions/ask", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionId, questionText: q.text,
          options: q.answers.map(a => a.text),
          timeLimitSeconds: 30,
          correctAnswerIndex: q.answers.findIndex(a => a.isCorrect),
        }),
      });
      if (!resp.ok) throw new Error(await resp.text());
      const data = await resp.json();
      setCurrentQuestionId(data.questionId);
      setMsg(`Pregunta ${currentIdx + 1}/${questions.length} enviada`);
    } catch (ex: unknown) {
      setMsg("Error: " + ((ex as Error)?.message || "desconocido"));
    } finally {
      setSending(false);
    }
  };

  const closeQuestion = async () => {
    if (!currentQuestionId) return;
    setClosing(true);
    try {
      await fetchWithAuth(`/api/trivia/questions/${currentQuestionId}/close`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sessionId }),
      });
      setQuestionClosed(true);
    } catch {
      setMsg("Error al cerrar la pregunta");
    } finally {
      setClosing(false);
    }
  };

  const goNext = () => {
    if (currentIdx < questions.length - 1) {
      setCurrentIdx(i => i + 1); setCurrentQuestionId(null); setAnswerCount(0); setMsg(""); setCooldown(0);
      setQuestionClosed(false); onClearResults();
      prevAllAnswered.current = false;
    } else {
      setMsg("¡Todas las preguntas enviadas!");
    }
  };

  if (!questions.length) return <p style={{ color: "#555", fontSize: "0.875rem" }}>Cargando preguntas...</p>;

  const q = questions[currentIdx];

  return (
    <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "1rem" }}>
      <div style={{ fontSize: "0.78rem", color: "#888", marginBottom: "0.5rem", textTransform: "uppercase", letterSpacing: 1 }}>
        Pregunta {currentIdx + 1} de {questions.length}
      </div>
      <div style={{ fontWeight: 600, color: "white", marginBottom: "0.5rem", lineHeight: 1.4 }}>{q.text}</div>
      <div style={{ color: "#888", fontSize: "0.82rem", marginBottom: "0.75rem" }}>
        {q.answers.map((a, i) => (
          <span key={a.id} style={{ marginRight: 12 }}>
            {String.fromCharCode(65 + i)}. {a.text}{a.isCorrect ? " ✓" : ""}
          </span>
        ))}
      </div>
      {currentQuestionId && totalParticipants > 0 && (
        <div style={{ color: "#ccc", fontSize: "0.82rem", marginBottom: "0.75rem" }}>
          Respondieron: {answerCount} / {totalParticipants}
          {allAnswered && cooldown > 0 && <span style={{ color: "#fbbf24", marginLeft: 8 }}>— revisando ({cooldown}s)</span>}
          {allAnswered && cooldown === 0 && <span style={{ color: "#4caf50", marginLeft: 8 }}>✅ Todos respondieron</span>}
        </div>
      )}
      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
        {!currentQuestionId ? (
          <button onClick={sendQuestion} disabled={sending} style={{ padding: "8px 18px", backgroundColor: sending ? "#555" : "#e94560", color: "white", border: "none", borderRadius: 6, cursor: sending ? "not-allowed" : "pointer", fontWeight: 600, fontSize: "0.875rem" }}>
            {sending ? "Enviando..." : "Enviar Pregunta"}
          </button>
        ) : !questionClosed ? (
          <button onClick={closeQuestion} disabled={closing || !canGoNext} style={{ padding: "8px 18px", backgroundColor: closing ? "#555" : canGoNext ? "#1a2d4a" : "#2a2a2a", color: closing ? "#888" : canGoNext ? "#3b82f6" : "#555", border: `1px solid ${canGoNext ? "#3b82f6" : "#333"}`, borderRadius: 6, cursor: (closing || !canGoNext) ? "not-allowed" : "pointer", fontWeight: 600, fontSize: "0.875rem" }}>
            {closing ? "Cerrando..." : !allAnswered ? "Esperando respuestas..." : cooldown > 0 ? `Cerrar en ${cooldown}s` : "Cerrar Pregunta"}
          </button>
        ) : currentIdx < questions.length - 1 ? (
          <button onClick={goNext} style={{ padding: "8px 18px", backgroundColor: "#1a4d2e", color: "#4caf50", border: "1px solid #4caf50", borderRadius: 6, cursor: "pointer", fontWeight: 600, fontSize: "0.875rem" }}>
            Siguiente →
          </button>
        ) : (
          <div style={{ color: "#4caf50", fontSize: "0.82rem", padding: "8px 0" }}>✅ Quiz completado</div>
        )}
      </div>
      {questionClosed && questionResults.length > 0 && (
        <div style={{ marginTop: "0.75rem", backgroundColor: "#0d1b35", borderRadius: 6, padding: "0.75rem" }}>
          <div style={{ fontSize: "0.72rem", color: "#888", fontWeight: 700, textTransform: "uppercase" as const, letterSpacing: 1, marginBottom: "0.5rem" }}>Distribución de respuestas</div>
          {questionResults.map((r, i) => (
            <div key={r.answerId} style={{ marginBottom: "0.45rem" }}>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "0.8rem", color: "#ccc", marginBottom: 3 }}>
                <span>{String.fromCharCode(65 + i)}. {r.text}</span>
                <span style={{ color: "#fbbf24", fontWeight: 700 }}>{r.count} ({r.percentage.toFixed(0)}%)</span>
              </div>
              <div style={{ height: 6, backgroundColor: "#1a2a4a", borderRadius: 3, overflow: "hidden" }}>
                <div style={{ height: "100%", width: `${r.percentage}%`, backgroundColor: "#3b82f6", borderRadius: 3, transition: "width 0.5s ease" }} />
              </div>
            </div>
          ))}
        </div>
      )}
      {msg && <div style={{ marginTop: "0.5rem", color: msg.includes("Error") ? "#e94560" : "#4caf50", fontSize: "0.82rem" }}>{msg}</div>}
    </div>
  );
}

// ── QR Download ───────────────────────────────────────────────────────────────

function QrDownloadRow({ stage }: { stage: SessionStage }) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);

  const qrContent = JSON.stringify({ stageId: stage.missionStageId, token: stage.qrToken });

  const handleDownload = useCallback(async () => {
    setGenerating(true);
    try {
      const dataUrl = await QRCode.toDataURL(qrContent, {
        width: 512,
        margin: 2,
        color: { dark: "#000000", light: "#ffffff" },
      });
      const link = document.createElement("a");
      link.href = dataUrl;
      link.download = `QR_Etapa_${stage.order}_${stage.missionTitle?.replace(/\s+/g, "_") ?? "etapa"}.png`;
      link.click();
    } catch { /* ignore */ } finally {
      setGenerating(false);
    }
  }, [qrContent, stage.order, stage.missionTitle]);

  const handlePreview = useCallback(async () => {
    if (previewUrl) { setPreviewUrl(null); return; }
    try {
      const dataUrl = await QRCode.toDataURL(qrContent, { width: 200, margin: 2 });
      setPreviewUrl(dataUrl);
    } catch { /* ignore */ }
  }, [qrContent, previewUrl]);

  return (
    <div style={{ border: "1px solid #0f3460", borderRadius: 7, padding: "0.75rem 1rem", backgroundColor: "#0d1b35" }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "0.75rem", flexWrap: "wrap" as const }}>
        <div>
          <span style={{ fontWeight: 700, color: "white", fontSize: "0.9rem" }}>Etapa {stage.order}</span>
          <span style={{ color: "#888", fontSize: "0.82rem", marginLeft: 8 }}>{stage.missionTitle}</span>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <button onClick={handlePreview} style={{
            padding: "6px 14px", backgroundColor: "#16213e", color: "#ccc",
            border: "1px solid #0f3460", borderRadius: 6, cursor: "pointer", fontSize: "0.8rem",
          }}>
            {previewUrl ? "Ocultar" : "Vista previa"}
          </button>
          <button onClick={handleDownload} disabled={generating} style={{
            padding: "6px 14px", backgroundColor: generating ? "#2a2a2a" : "#1a4d2e",
            color: generating ? "#555" : "#4caf50", border: `1px solid ${generating ? "#333" : "#4caf50"}`,
            borderRadius: 6, cursor: generating ? "not-allowed" : "pointer", fontSize: "0.8rem", fontWeight: 700,
          }}>
            {generating ? "Generando..." : "Descargar QR"}
          </button>
        </div>
      </div>
      {previewUrl && (
        <div style={{ marginTop: "0.75rem", textAlign: "center" }}>
          <img src={previewUrl} alt={`QR Etapa ${stage.order}`} style={{ width: 160, height: 160, imageRendering: "pixelated", borderRadius: 4, border: "3px solid white" }} />
          <p style={{ color: "#666", fontSize: "0.75rem", marginTop: 4 }}>Etapa {stage.order} — {stage.missionTitle}</p>
        </div>
      )}
    </div>
  );
}

// ── Helpers ────────────────────────────────────────────────────────────────────

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const sec = seconds % 60;
  return `${m.toString().padStart(2, "0")}:${sec.toString().padStart(2, "0")}`;
}

function getTransitions(status: SessionStatus): string[] {
  switch (status) {
    case "Scheduled": return ["Preparing", "Cancelled"];
    case "Preparing": return ["Active", "Cancelled"];
    case "Active": return ["Paused", "Finished", "Cancelled"];
    case "Paused": return ["Active", "Finished", "Cancelled"];
    default: return [];
  }
}

function transitionLabel(t: string): string {
  switch (t) {
    case "Preparing": return "Preparar";
    case "Active": return "▶ Iniciar";
    case "Paused": return "⏸ Pausar";
    case "Finished": return "Finalizar";
    case "Cancelled": return "Cancelar";
    default: return t;
  }
}
