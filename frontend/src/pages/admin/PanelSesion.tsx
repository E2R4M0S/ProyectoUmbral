import { useState, useEffect, useRef, useCallback } from "react";
import { useParams, Link, useLocation } from "react-router-dom";
import QRCode from "qrcode";
import { getSessionProgress, getSessionById, transitionSession, advanceStage, ApiError } from "../../services/sessionsApi";
import { getMissionById } from "../../services/missionsApi";
import { fetchWithAuth } from "../../services/api";
import { getSessionTeams, createSessionTeam, removeTeamMember } from "../../services/sessionTeamsApi";
import { computeMissionRemaining } from "../../utils/missionTimer";
import { useSignalR } from "../../hooks/useSignalR";
import type { AnswerResult } from "../../hooks/useSignalR";
import type { SessionProgress, ParticipantProgress, SessionStatus, SessionStage } from "../../types/session";
import type { MissionDetail, Clue } from "../../types/mission";
import type { RankingEntry } from "../../types/game";
import type { SessionTeam } from "../../services/sessionTeamsApi";

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
  // RB-10: el admin puede ver el detalle completo de cualquier sesión, pero no administrarla.
  const readOnly = basePath === "/admin";

  const [progress, setProgress] = useState<SessionProgress | null>(null);
  const [pin, setPin] = useState<string>("");
  const [stages, setStages] = useState<SessionStage[]>([]);
  const [currentStageOrder, setCurrentStageOrder] = useState(0);
  const [mission, setMission] = useState<MissionDetail | null>(null);
  const [selectedClueId, setSelectedClueId] = useState<string>("");
  const [clueMode, setClueMode] = useState<"predefined" | "custom">("predefined");
  const [customClueText, setCustomClueText] = useState("");
  const [customCluePenalty, setCustomCluePenalty] = useState("");
  const [releasing, setReleasing] = useState(false);
  const [clueMsg, setClueMsg] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [localSeconds, setLocalSeconds] = useState(0);
  // Seconds since the CURRENT mission started (server-anchored) — drives the mission countdown,
  // separate from localSeconds (whole-session elapsed, used only for the total-duration auto-finish).
  const [localMissionSeconds, setLocalMissionSeconds] = useState(0);
  const lastServerRef = useRef(0);
  const autoFinishedRef = useRef(false);
  const [selectedQuizId, setSelectedQuizId] = useState<string>("");
  const [advancing, setAdvancing] = useState(false);
  const [advanceMsg, setAdvanceMsg] = useState("");
  const [ranking, setRanking] = useState<RankingEntry[]>([]);
  const [questionResults, setQuestionResults] = useState<AnswerResult[]>([]);
  const [finalRanking, setFinalRanking] = useState<RankingEntry[]>([]);
  const [questionTimer, setQuestionTimer] = useState<number | null>(null);
  const [teams, setTeams] = useState<SessionTeam[]>([]);

  async function load() {
    if (!id) return;
    try {
      const p = await getSessionProgress(id);
      setProgress(p);
      lastServerRef.current = p.elapsedSeconds;
      setLocalSeconds(p.elapsedSeconds);
      setLocalMissionSeconds(p.currentMissionElapsedSeconds);
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
      // Pistas de ESTA etapa únicamente — no de todas las etapas de la misión.
      const firstClue = m.stages?.find(st => st.id === current.missionStageId)?.clues?.[0];
      setSelectedClueId(firstClue?.id ?? "");
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
    if (!id) return;
    getSessionTeams(id).then(setTeams).catch(() => {});
    const i = setInterval(() => getSessionTeams(id).then(setTeams).catch(() => {}), 5000);
    return () => clearInterval(i);
  }, [id]);

  useEffect(() => {
    if (!progress || progress.status !== "Active") return;
    const tick = setInterval(() => {
      setLocalSeconds(prev => prev + 1);
      setLocalMissionSeconds(prev => prev + 1);
    }, 1000);
    return () => clearInterval(tick);
  }, [progress?.status]);

  useEffect(() => {
    if (progress) setLocalSeconds(progress.elapsedSeconds);
  }, [progress?.elapsedSeconds]);

  useEffect(() => {
    if (progress) setLocalMissionSeconds(progress.currentMissionElapsedSeconds);
  }, [progress?.currentMissionElapsedSeconds]);

  // Auto-finish when remaining time reaches 0 (RF-02 already enforces this server-side too —
  // this is just for a snappier UI update on the operator's own screen, so it's pointless
  // for a read-only admin viewer, and the ownership check would reject it anyway).
  useEffect(() => {
    if (readOnly || !id || progress?.status !== "Active" || autoFinishedRef.current) return;
    const totalSeconds = progress?.totalDurationSeconds ?? 0;
    if (totalSeconds <= 0 || localSeconds < totalSeconds) return;
    autoFinishedRef.current = true;
    transitionSession(id, "Finished").then(() => load()).catch(() => {});
  }, [localSeconds, progress?.status, progress?.totalDurationSeconds, id]);

  useEffect(() => {
    const stage = stages.find(st => st.order === currentStageOrder + 1);
    if (!stage || stage.missionType !== "Trivia") {
      setQuestionTimer(null);
      return;
    }
    const rawId = stage.missionStageId ?? stage.quizId ?? "";
    const isEmpty = !rawId || rawId === "00000000-0000-0000-0000-000000000000";
    if (!isEmpty) setSelectedQuizId(rawId);
  }, [currentStageOrder, stages]);

  useEffect(() => {
    if (progress?.status !== "Finished" || !id) return;
    // Use real-time ranking if already populated, else load from session detail
    if (ranking.length > 0) {
      setFinalRanking(ranking);
      return;
    }
    fetchWithAuth(`/api/sessions/${id}/ranking`)
      .then(r => r.json())
      .then((data: { type: string; displayName: string; score: number; teamId?: string; userId?: string }[]) => {
        setFinalRanking(data.map((e, i) => ({
          position: i + 1,
          teamName: e.displayName,
          score: e.score,
          userId: e.userId,
        })));
      })
      .catch(() => {});
  }, [progress?.status, id]);

  // Stage can now auto-advance server-side (Treasure QR scans, trivia round closing) without
  // this operator ever clicking anything — re-pull stages/currentStageOrder when that happens
  // so the dashboard (and its per-mission timer) follows real progress instead of lagging.
  const refreshStageFromServer = useCallback(async () => {
    if (!id) return;
    try {
      const detail = await getSessionById(id);
      const newOrder = detail.currentStageOrder ?? 0;
      setStages(detail.stages ?? []);
      setCurrentStageOrder(newOrder);
      await loadMissionForStage(detail.stages ?? [], newOrder);
    } catch { /* ignore */ }
  }, [id]);

  useSignalR({
    sessionId: id ?? "",
    onStatusChanged: () => { load(); },
    onProgressUpdated: (data) => {
      const p = data as { stageAdvanced?: boolean };
      if (p?.stageAdvanced) { refreshStageFromServer(); load(); }
    },
    onClueReleased: () => {},
    onConnectionStateChange: () => {},
    onRankingUpdated: (incoming) => { setRanking(incoming); setFinalRanking(incoming); },
    onQuestionResultsUpdated: (_, results) => { setQuestionResults(results); },
  });

  // A team created but never joined by anyone can't ever answer a trivia question — counting it
  // as an expected responder means "esperando respuestas" never reaches 100% and the round never
  // auto-closes. Only non-empty teams, plus solo participants who aren't in any team, count.
  const nonEmptyTeams = teams.filter(t => t.memberCount > 0);
  const teamMemberIds = new Set(teams.flatMap(t => t.members.map(m => m.userId)));
  const soloParticipantCount = Math.max(0, (progress?.participants?.length || 0) - teamMemberIds.size);
  const expectedResponders = nonEmptyTeams.length > 0
    ? nonEmptyTeams.length + soloParticipantCount
    : (progress?.participants?.length || 0);

  const currentStage = stages.find(st => st.order === currentStageOrder + 1);
  // Solo las pistas de la etapa actual — nunca las de otras etapas de la misma misión.
  const currentMissionStage = mission?.stages?.find(st => st.id === currentStage?.missionStageId);
  const stageClues: Clue[] = currentMissionStage?.clues ?? [];
  const selectedClue = stageClues.find(c => c.id === selectedClueId);
  const isLastStage = stages.length === 0 || currentStageOrder >= stages.length - 1;
  const canAdvance = progress?.status === "Active" && !isLastStage;
  const isTreasure = currentStage?.missionType === "Treasure";
  const isTerminal = progress?.status === "Finished" || progress?.status === "Cancelled";
  const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";
  const isQuizPreset = currentStage?.missionType === "Trivia" &&
    Boolean(currentStage?.missionStageId) &&
    currentStage?.missionStageId !== EMPTY_GUID;

  // Same algorithm the participant's Timer uses (computeMissionRemaining), fed the same
  // server-anchored mission-elapsed seconds, so operator and participants stay in sync.
  const treasureRemaining: number | null = isTreasure
    ? computeMissionRemaining(currentStage?.timeMinutes, localMissionSeconds)
    : null;

  // Once a mission's time budget is spent, keep advancing stage-by-stage (treasureRemaining
  // stays 0 for every remaining stage of that mission) until reaching the next mission.
  const treasureAdvancingRef = useRef(false);
  useEffect(() => {
    if (readOnly || treasureRemaining !== 0 || !currentStage?.missionId || progress?.status !== "Active") return;
    if (isLastStage || advancing || treasureAdvancingRef.current) return;
    treasureAdvancingRef.current = true;
    handleAdvanceStage().finally(() => { treasureAdvancingRef.current = false; });
  }, [treasureRemaining, currentStage?.missionId, progress?.status, isLastStage, advancing]);


  const handleReleaseClue = async () => {
    const isCustom = clueMode === "custom";
    if (isCustom ? !customClueText.trim() : !selectedClueId) return;
    setReleasing(true);
    setClueMsg("");
    try {
      const body = isCustom
        ? { content: customClueText.trim(), penalty: customCluePenalty ? Number(customCluePenalty) : null, teamId: null }
        : { clueId: selectedClueId, teamId: null };
      const resp = await fetchWithAuth(`/api/sessions/${id}/clues/release`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      if (!resp.ok) throw new Error();
      const result = await resp.json();
      setClueMsg(result.hasContent ? "Pista enviada correctamente." : "Pista enviada (sin contenido definido).");
      if (isCustom) { setCustomClueText(""); setCustomCluePenalty(""); }
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
          {readOnly && (
            <span style={css.chip("#555")} title="El admin consulta el detalle de la sesión pero no puede administrarla">
              👁 Solo lectura
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

      {/* Timer — treasure mission countdown or trivia question countdown, never both */}
      {(() => {
        if (isTreasure && treasureRemaining !== null && progress?.status === "Active") {
          const color = treasureRemaining <= 60 ? "#e94560" : treasureRemaining <= 300 ? "#fbbf24" : "#34d399";
          return (
            <div style={css.timerBox}>
              <div style={{ ...css.timerValue, color }}>{formatTime(treasureRemaining)}</div>
              <div style={css.timerLabel}>tiempo restante de misión</div>
            </div>
          );
        }
        if (!isTreasure && questionTimer !== null) {
          const color = questionTimer <= 5 ? "#e94560" : questionTimer <= 10 ? "#fbbf24" : "#34d399";
          return (
            <div style={css.timerBox}>
              <div style={{ ...css.timerValue, color }}>{formatTime(questionTimer)}</div>
              <div style={css.timerLabel}>tiempo de pregunta</div>
            </div>
          );
        }
        return null;
      })()}

      {/* Transition buttons */}
      {!readOnly && !isTerminal && getTransitions(progress.status as SessionStatus).length > 0 && (
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
              {!readOnly && !isTerminal && (
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

      {/* Teams */}
      <TeamsManager
        sessionId={id!}
        teams={teams}
        sessionStatus={progress.status}
        readOnly={readOnly}
        onTeamsChanged={() => id && getSessionTeams(id).then(setTeams).catch(() => {})}
      />

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

      {/* Clues — only for Treasure, and only for the current stage */}
      {isTreasure && !isTerminal && readOnly && (
        <>
          <div style={css.sectionTitle}>Pistas de esta etapa</div>
          <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "1rem" }}>
            {stageClues.length === 0 ? (
              <p style={{ color: "#555", fontSize: "0.875rem" }}>Esta etapa no tiene pistas predefinidas.</p>
            ) : (
              <div style={{ display: "flex", flexDirection: "column", gap: "0.6rem" }}>
                {stageClues.map(clue => (
                  <div key={clue.id} style={{ padding: "0.75rem", backgroundColor: "#0d1b35", borderRadius: 6, fontSize: "0.875rem", color: "#ccc", lineHeight: 1.5 }}>
                    {clue.content}
                    {clue.penalty != null && (
                      <span style={{ color: "#e94560", marginLeft: 8, fontSize: "0.78rem" }}>−{clue.penalty} pts</span>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        </>
      )}

      {isTreasure && !isTerminal && !readOnly && (
        <>
          <div style={css.sectionTitle}>Pistas de esta etapa</div>
          <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "1rem" }}>
            <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1rem" }}>
              <button
                onClick={() => setClueMode("predefined")}
                style={clueMode === "predefined" ? css.btnBlue : css.btnGhost}
              >
                Predefinidas{stageClues.length > 0 ? ` (${stageClues.length})` : ""}
              </button>
              <button
                onClick={() => setClueMode("custom")}
                style={clueMode === "custom" ? css.btnBlue : css.btnGhost}
              >
                ✏️ Crear en vivo
              </button>
            </div>

            {clueMode === "predefined" ? (
              stageClues.length === 0 ? (
                <p style={{ color: "#555", fontSize: "0.875rem", marginBottom: "0.75rem" }}>
                  Esta etapa no tiene pistas predefinidas — usá "Crear en vivo" para mandar una ahora.
                </p>
              ) : (
                <>
                  <select value={selectedClueId} onChange={e => setSelectedClueId(e.target.value)} style={css.select}>
                    {stageClues.map(clue => (
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
                </>
              )
            ) : (
              <>
                <textarea
                  value={customClueText}
                  onChange={e => setCustomClueText(e.target.value)}
                  placeholder="Escribí la pista para enviar ahora mismo..."
                  rows={3}
                  style={{ ...css.select, resize: "vertical" as const, fontFamily: "inherit" }}
                />
                <input
                  type="number"
                  min={0}
                  value={customCluePenalty}
                  onChange={e => setCustomCluePenalty(e.target.value)}
                  placeholder="Penalización en puntos (opcional)"
                  style={css.select}
                />
              </>
            )}

            <button
              onClick={handleReleaseClue}
              disabled={releasing || (clueMode === "predefined" ? !selectedClueId : !customClueText.trim())}
              style={releasing ? css.btnDisabled : css.btnPrimary}
            >
              {releasing ? "Enviando..." : "🔔 Liberar Pista"}
            </button>
            {clueMsg && <div style={clueMsg.includes("Error") ? css.msgError : css.msgSuccess}>{clueMsg}</div>}
          </div>
        </>
      )}

      {/* Final results podio — HU-45 */}
      {progress.status === "Finished" && (
        <>
          <div style={css.sectionTitle}>Resultados Finales</div>
          {finalRanking.length === 0 ? (
            <p style={{ color: "#555", fontSize: "0.875rem" }}>Cargando resultados...</p>
          ) : (
            <>
              {/* Podio top 3 */}
              <div style={{ display: "flex", gap: "0.75rem", marginBottom: "1rem", alignItems: "flex-end", justifyContent: "center" }}>
                {[1, 0, 2].map(idx => {
                  const entry = finalRanking[idx];
                  if (!entry) return null;
                  const isFirst = entry.position === 1;
                  const medalColor = entry.position === 1 ? "#fbbf24" : entry.position === 2 ? "#9ca3af" : "#cd7f32";
                  const height = entry.position === 1 ? 100 : entry.position === 2 ? 80 : 65;
                  return (
                    <div key={idx} style={{ flex: 1, display: "flex", flexDirection: "column", alignItems: "center", gap: "0.4rem" }}>
                      <span style={{ fontSize: isFirst ? "1.75rem" : "1.25rem" }}>
                        {entry.position === 1 ? "🥇" : entry.position === 2 ? "🥈" : "🥉"}
                      </span>
                      <div style={{ textAlign: "center" }}>
                        <div style={{ fontWeight: 700, color: "white", fontSize: "0.85rem", wordBreak: "break-word" }}>{entry.teamName}</div>
                        <div style={{ fontWeight: 700, color: medalColor, fontSize: "0.9rem" }}>{entry.score} pts</div>
                      </div>
                      <div style={{ width: "100%", backgroundColor: medalColor, borderRadius: "6px 6px 0 0", height }} />
                    </div>
                  );
                })}
              </div>
              {/* Full list */}
              {finalRanking.length > 3 && (
                <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, overflow: "hidden", marginBottom: "1rem" }}>
                  {finalRanking.slice(3).map((entry, i) => (
                    <div key={i} style={{
                      display: "flex", alignItems: "center", justifyContent: "space-between",
                      padding: "0.55rem 1rem",
                      borderBottom: i < finalRanking.length - 4 ? "1px solid #0f3460" : "none",
                    }}>
                      <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
                        <span style={{ fontWeight: 700, minWidth: 24, color: "#555" }}>#{entry.position}</span>
                        <span style={{ color: "#ccc", fontSize: "0.875rem" }}>{entry.teamName}</span>
                      </div>
                      <span style={{ fontWeight: 700, color: "#e94560" }}>{entry.score} pts</span>
                    </div>
                  ))}
                </div>
              )}
            </>
          )}
        </>
      )}

      {/* Trivia controls */}
      {!isTreasure && !isTerminal && progress.status === "Active" && (
        <>
          {!readOnly && (
            <>
              <div style={css.sectionTitle}>Quiz de Trivia</div>
              {!isQuizPreset && <QuizSelector selectedQuizId={selectedQuizId} onSelect={setSelectedQuizId} />}
              {selectedQuizId && (
                <>
                  <div style={{ ...css.sectionTitle, marginTop: "1.25rem" }}>Enviar Preguntas</div>
                  <QuizQuestionSender sessionId={id!} quizId={selectedQuizId} totalParticipants={expectedResponders} questionResults={questionResults} onClearResults={() => setQuestionResults([])} onTimerUpdate={(s) => setQuestionTimer(s)} onQuizComplete={() => { if (!isLastStage) handleAdvanceStage(); }} />
                </>
              )}
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

function QuizQuestionSender({ sessionId, quizId, totalParticipants, questionResults, onClearResults, onTimerUpdate, onQuizComplete }: { sessionId: string; quizId: string; totalParticipants: number; questionResults: AnswerResult[]; onClearResults: () => void; onTimerUpdate?: (seconds: number | null) => void; onQuizComplete?: () => void }) {
  const [currentIdx, setCurrentIdx] = useState(0);
  const [questions, setQuestions] = useState<{ id: string; text: string; timeLimitSeconds: number; answers: { id: string; text: string; isCorrect: boolean }[] }[]>([]);
  const [sending, setSending] = useState(false);
  const [questionClosed, setQuestionClosed] = useState(false);
  const [msg, setMsg] = useState("");
  const [currentQuestionId, setCurrentQuestionId] = useState<string | null>(null);
  const [answerCount, setAnswerCount] = useState(0);
  const [autoSeconds, setAutoSeconds] = useState<number | null>(null);
  const [phase, setPhase] = useState<"idle" | "active" | "results" | "done">("idle");

  // Refs to read current values inside timer callbacks without stale closures
  const currentQuestionIdRef = useRef<string | null>(null);
  const questionClosedRef = useRef(false);
  const currentIdxRef = useRef(0);
  const questionsRef = useRef(questions);
  const doSendQuestionRef = useRef<((idx: number) => Promise<void>) | null>(null);
  const onTimerUpdateRef = useRef(onTimerUpdate);
  const onQuizCompleteRef = useRef(onQuizComplete);
  useEffect(() => { currentQuestionIdRef.current = currentQuestionId; }, [currentQuestionId]);
  useEffect(() => { questionClosedRef.current = questionClosed; }, [questionClosed]);
  useEffect(() => { currentIdxRef.current = currentIdx; }, [currentIdx]);
  useEffect(() => { questionsRef.current = questions; }, [questions]);
  useEffect(() => { onTimerUpdateRef.current = onTimerUpdate; }, [onTimerUpdate]);
  useEffect(() => { onQuizCompleteRef.current = onQuizComplete; }, [onQuizComplete]);

  // Notify parent when the last question has been closed, so it can move the
  // session on to the next mission (mirrors the Treasure mission time-up auto-advance).
  useEffect(() => {
    if (phase === "done") onQuizCompleteRef.current?.();
  }, [phase]);

  useEffect(() => {
    if (!quizId) return;
    setCurrentIdx(0); currentIdxRef.current = 0;
    setCurrentQuestionId(null); currentQuestionIdRef.current = null;
    setAnswerCount(0); setMsg(""); setPhase("idle");
    setQuestionClosed(false); questionClosedRef.current = false;
    fetchWithAuth(`/api/quizzes/${quizId}`).then(r => r.json())
      .then(data => setQuestions(data.questions || []))
      .catch(() => setMsg("Error al cargar preguntas"));
  }, [quizId]);

  // Poll answer count while question is active
  useEffect(() => {
    if (!currentQuestionId || questionClosed) return;
    const interval = setInterval(async () => {
      try {
        const resp = await fetchWithAuth(`/api/trivia/questions/${currentQuestionId}/answer-count`);
        if (resp.ok) { const data = await resp.json(); setAnswerCount(data.answerCount); }
      } catch { }
    }, 2000);
    return () => clearInterval(interval);
  }, [currentQuestionId, questionClosed]);

  const doCloseQuestion = useCallback(async (qId: string) => {
    if (questionClosedRef.current) return;
    questionClosedRef.current = true;
    try {
      await fetchWithAuth(`/api/trivia/questions/${qId}/close`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sessionId }),
      });
      setQuestionClosed(true);
      setPhase("results");
    } catch {
      questionClosedRef.current = false;
      setMsg("Error al cerrar la pregunta");
    }
  }, [sessionId]);

  // Auto-close when all participants have answered
  useEffect(() => {
    if (!currentQuestionId || questionClosed || totalParticipants <= 0 || answerCount < totalParticipants) return;
    doCloseQuestion(currentQuestionId);
  }, [answerCount, totalParticipants, currentQuestionId, questionClosed, doCloseQuestion]);

  const doSendQuestion = useCallback(async (idx: number) => {
    const qs = questionsRef.current;
    if (!qs.length || idx >= qs.length) return;
    const q = qs[idx];
    setSending(true);
    setCurrentQuestionId(null); currentQuestionIdRef.current = null;
    setAnswerCount(0); setMsg(""); setAutoSeconds(null);
    setQuestionClosed(false); questionClosedRef.current = false;
    setPhase("active");
    onClearResults();
    try {
      const resp = await fetchWithAuth("/api/trivia/questions/ask", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionId, questionText: q.text,
          options: q.answers.map(a => a.text),
          timeLimitSeconds: q.timeLimitSeconds || 30,
          correctAnswerIndex: q.answers.findIndex(a => a.isCorrect),
        }),
      });
      if (!resp.ok) throw new Error(await resp.text());
      const data = await resp.json();
      setCurrentQuestionId(data.questionId);
      currentQuestionIdRef.current = data.questionId;
    } catch (ex: unknown) {
      setMsg("Error: " + ((ex as Error)?.message || "desconocido"));
      setPhase("idle");
    } finally {
      setSending(false);
    }
  }, [sessionId, onClearResults]);
  useEffect(() => { doSendQuestionRef.current = doSendQuestion; }, [doSendQuestion]);

  // Notify parent of question timer state
  useEffect(() => {
    if (!currentQuestionId || questionClosed) {
      onTimerUpdateRef.current?.(null);
    } else {
      onTimerUpdateRef.current?.(autoSeconds);
    }
  }, [autoSeconds, currentQuestionId, questionClosed]);

  // Auto-close countdown when a question is active
  useEffect(() => {
    if (!currentQuestionId || questionClosed) return;
    const q = questionsRef.current[currentIdxRef.current];
    const LIMIT = q?.timeLimitSeconds || 30;
    let timeLeft = LIMIT;
    setAutoSeconds(timeLeft);
    const timer = setInterval(() => {
      timeLeft--;
      setAutoSeconds(timeLeft);
      if (timeLeft <= 0) {
        clearInterval(timer);
        const qId = currentQuestionIdRef.current;
        if (qId) doCloseQuestion(qId);
      }
    }, 1000);
    return () => clearInterval(timer);
  }, [currentQuestionId, doCloseQuestion]);

  // Auto-advance immediately after question closes: send next question with no delay.
  useEffect(() => {
    if (phase !== "results") return;
    const timer = setTimeout(() => {
      const nextIdx = currentIdxRef.current + 1;
      const qs = questionsRef.current;
      if (nextIdx < qs.length) {
        setCurrentIdx(nextIdx); currentIdxRef.current = nextIdx;
        doSendQuestionRef.current?.(nextIdx);
      } else {
        setPhase("done");
        setCurrentQuestionId(null); currentQuestionIdRef.current = null;
      }
    }, 0);
    return () => clearTimeout(timer);
  }, [phase]);

  if (!questions.length) return <p style={{ color: "#555", fontSize: "0.875rem" }}>Cargando preguntas...</p>;

  const q = questions[currentIdx];
  const allAnswered = totalParticipants > 0 && answerCount >= totalParticipants;

  return (
    <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "1rem" }}>
      <div style={{ fontSize: "0.78rem", color: "#888", marginBottom: "0.5rem", textTransform: "uppercase", letterSpacing: 1 }}>
        Pregunta {currentIdx + 1} de {questions.length}
      </div>

      {/* Auto-close countdown */}
      {currentQuestionId && !questionClosed && autoSeconds !== null && (
        <div style={{ fontSize: "0.82rem", color: autoSeconds <= 5 ? "#e94560" : autoSeconds <= 10 ? "#fbbf24" : "#aaa", marginBottom: "0.5rem", fontWeight: autoSeconds <= 10 ? 700 : 400 }}>
          ⏱ Se cierra en {autoSeconds}s
        </div>
      )}

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
          {allAnswered && <span style={{ color: "#4caf50", marginLeft: 8 }}>✅ Todos respondieron</span>}
        </div>
      )}

      <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", alignItems: "center" }}>
        {phase === "idle" && (
          <button onClick={() => doSendQuestion(0)} disabled={sending} style={{ padding: "8px 18px", backgroundColor: sending ? "#555" : "#e94560", color: "white", border: "none", borderRadius: 6, cursor: sending ? "not-allowed" : "pointer", fontWeight: 600, fontSize: "0.875rem" }}>
            {sending ? "Enviando..." : "Iniciar Quiz"}
          </button>
        )}
        {phase === "active" && currentQuestionId && !questionClosed && (
          <button onClick={() => doCloseQuestion(currentQuestionId)} style={{ padding: "6px 14px", backgroundColor: "transparent", color: "#3b82f6", border: "1px solid #3b82f6", borderRadius: 6, cursor: "pointer", fontSize: "0.8rem" }}>
            Cerrar ya
          </button>
        )}
        {phase === "done" && (
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
      {msg && <div style={{ marginTop: "0.5rem", color: msg.includes("Error") ? "#e94560" : "#888", fontSize: "0.82rem" }}>{msg}</div>}
    </div>
  );
}

// ── Teams Manager ─────────────────────────────────────────────────────────────

function TeamsManager({ sessionId, teams, sessionStatus, readOnly, onTeamsChanged }: {
  sessionId: string;
  teams: SessionTeam[];
  sessionStatus: string;
  readOnly: boolean;
  onTeamsChanged: () => void;
}) {
  const [newName, setNewName] = useState("");
  const [creating, setCreating] = useState(false);
  const [createMsg, setCreateMsg] = useState("");
  const canCreate = !readOnly && (sessionStatus === "Scheduled" || sessionStatus === "Preparing");

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) return;
    setCreating(true);
    setCreateMsg("");
    try {
      await createSessionTeam(sessionId, newName.trim());
      setNewName("");
      setCreateMsg("✓ Equipo creado.");
      onTeamsChanged();
    } catch (err) {
      setCreateMsg(err instanceof Error ? err.message : "Error al crear equipo.");
    } finally {
      setCreating(false);
    }
  };

  const handleRemoveMember = async (teamId: string, userId: string) => {
    try {
      await removeTeamMember(sessionId, teamId, userId);
      onTeamsChanged();
    } catch { /* ignore */ }
  };

  return (
    <>
      <div style={css.sectionTitle}>Equipos ({teams.length})</div>

      {canCreate && (
        <form onSubmit={handleCreate} style={{ display: "flex", gap: "0.5rem", marginBottom: "1rem" }}>
          <input
            value={newName}
            onChange={e => setNewName(e.target.value)}
            placeholder="Nombre del equipo..."
            maxLength={100}
            style={{ ...css.select, marginBottom: 0, flex: 1 }}
          />
          <button type="submit" disabled={creating || !newName.trim()} style={creating ? css.btnDisabled : css.btnPrimary}>
            {creating ? "Creando..." : "+ Equipo"}
          </button>
        </form>
      )}
      {createMsg && (
        <div style={createMsg.startsWith("✓") ? css.msgSuccess : css.msgError}>
          {createMsg}
        </div>
      )}

      {teams.length === 0 ? (
        <p style={{ color: "#555", fontSize: "0.875rem" }}>
          {canCreate ? "Crea equipos antes de iniciar la sesión." : "No hay equipos en esta sesión."}
        </p>
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem", marginBottom: "1rem" }}>
          {teams.map(team => (
            <div key={team.id} style={{ ...css.stageCard(""), padding: "0.875rem 1rem" }}>
              <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "0.5rem" }}>
                <span style={{ fontWeight: 700, color: "white" }}>{team.name}</span>
                <span style={{ color: "#888", fontSize: "0.8rem" }}>
                  {team.memberCount}/{team.maxMembers} miembros
                </span>
              </div>
              {team.members.length === 0 ? (
                <p style={{ color: "#555", fontSize: "0.8rem", margin: 0 }}>Sin miembros todavía</p>
              ) : (
                <div style={{ display: "flex", flexWrap: "wrap", gap: "0.4rem" }}>
                  {team.members.map(m => (
                    <span key={m.userId} style={{ display: "inline-flex", alignItems: "center", gap: "0.4rem", padding: "2px 10px", backgroundColor: "#0d1b35", border: "1px solid #0f3460", borderRadius: 20, fontSize: "0.8rem", color: "#ccc" }}>
                      {m.userAlias}
                      {canCreate && (
                        <button
                          onClick={() => handleRemoveMember(team.id, m.userId)}
                          style={{ background: "none", border: "none", color: "#e94560", cursor: "pointer", padding: 0, fontSize: "0.75rem", lineHeight: 1 }}
                          title="Quitar del equipo"
                        >
                          ✕
                        </button>
                      )}
                    </span>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </>
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
