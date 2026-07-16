import { useState, useEffect, useRef, useCallback } from "react";
import { useParams } from "react-router-dom";
import { useGame } from "../../../contexts/GameContext";
import { Timer } from "../../../components/game/Timer";
import { ClueCard } from "../../../components/game/ClueCard";
import { RankingBoard } from "../../../components/game/RankingBoard";
import { QuestionCard } from "../../../components/game/QuestionCard";
import { QrScanner } from "../../../components/QrScanner";
import { validateQr } from "../../../services/sessionsApi";
import { getSessionTeams } from "../../../services/sessionTeamsApi";
import { userManager } from "../../../auth/keycloak";

type ScanPhase = "idle" | "loading" | "success" | "error" | "waiting_gate" | "gate_opened" | "eliminated";

export function ActiveGame() {
  const { state, dispatch } = useGame();
  const { sessionId } = useParams<{ sessionId: string }>();

  const isTreasure = state.currentMissionType === "Treasure";

  const currentStageInfo = state.stages.find(s => s.order === state.participantStageOrder);
  const missionStages = currentStageInfo
    ? state.stages.filter(s => s.missionId === currentStageInfo.missionId).sort((a, b) => a.order - b.order)
    : [];
  const missionStageIndex = missionStages.findIndex(s => s.order === state.participantStageOrder) + 1;
  const missionStageTotal = missionStages.length;
  const missionTitle = currentStageInfo?.missionTitle ?? state.sessionName;

  // ── QR scan state ────────────────────────────────────────────────────────────
  const [scanPhase, setScanPhase] = useState<ScanPhase>("idle");
  const [scanError, setScanError] = useState("");
  const [cameraActive, setCameraActive] = useState(false);
  const [gateInfo, setGateInfo] = useState<{ position: number; threshold: number } | null>(null);
  // True once this participant scanned the very last stage of the whole session — they stay
  // mounted here (not navigated away) so the session's "Finished" status can flip GameView to
  // the real final results screen instead of a dead-end "mission complete" page.
  const [participantCompleted, setParticipantCompleted] = useState(false);

  const scanPhaseRef = useRef<ScanPhase>("idle");
  useEffect(() => { scanPhaseRef.current = scanPhase; }, [scanPhase]);

  // Load team on mount if context lost it (e.g. browser with expired API token); retry every 3s
  useEffect(() => {
    if (!sessionId) return;
    let cancelled = false;
    const restore = async () => {
      if (cancelled) return;
      // 1. Try sessionStorage first (set by WaitingRoom)
      try {
        const saved = sessionStorage.getItem(`myTeam_${sessionId}`);
        if (saved) {
          const teamData = JSON.parse(saved);
          if (!cancelled) dispatch({ type: "MY_IDENTITY_LOADED", userId: state.myUserId ?? "", team: teamData });
          return true;
        }
      } catch { /* ignore */ }
      // 2. Try localStorage (also set by WaitingRoom)
      try {
        const saved = localStorage.getItem(`myTeam_${sessionId}`);
        if (saved) {
          const teamData = JSON.parse(saved);
          try { sessionStorage.setItem(`myTeam_${sessionId}`, saved); } catch { /* ignore */ }
          if (!cancelled) dispatch({ type: "MY_IDENTITY_LOADED", userId: state.myUserId ?? "", team: teamData });
          return true;
        }
      } catch { /* ignore */ }
      // 3. Fall back to API (retried until OIDC is ready)
      try {
        const user = await userManager.getUser();
        const userId = user?.profile?.sub as string | undefined;
        if (!userId) return false; // OIDC not ready — will retry
        const teams = await getSessionTeams(sessionId);
        const myTeam = teams.find(t => t.members.some(m => m.userId === userId));
        if (myTeam) {
          const teamData = { id: myTeam.id, name: myTeam.name, memberIds: myTeam.members.map(m => m.userId) };
          try { sessionStorage.setItem(`myTeam_${sessionId}`, JSON.stringify(teamData)); } catch { /* ignore */ }
          try { localStorage.setItem(`myTeam_${sessionId}`, JSON.stringify(teamData)); } catch { /* ignore */ }
          if (!cancelled) dispatch({ type: "MY_IDENTITY_LOADED", userId, team: teamData });
          return true;
        }
      } catch { /* ignore */ }
      return false;
    };
    let interval: ReturnType<typeof setInterval> | null = null;
    restore().then(found => {
      if (!found && !cancelled) {
        interval = setInterval(async () => {
          if (cancelled) { if (interval) clearInterval(interval); return; }
          const ok = await restore();
          if (ok && interval) clearInterval(interval);
        }, 3000);
      }
    });
    return () => {
      cancelled = true;
      if (interval) clearInterval(interval);
    };
  }, [sessionId]); // eslint-disable-line

  // Refs to avoid stale closures in event handlers
  const myTeamIdRef = useRef(state.myTeam?.id);
  useEffect(() => { myTeamIdRef.current = state.myTeam?.id; }, [state.myTeam?.id]);
  const participantStageOrderRef = useRef(state.participantStageOrder);
  useEffect(() => { participantStageOrderRef.current = state.participantStageOrder; }, [state.participantStageOrder]);

  // Team QR sync: when a teammate scans their QR, advance our stage too
  useEffect(() => {
    const handler = (e: Event) => {
      const detail = (e as CustomEvent<{ teamId: string; newStageOrder: number; totalStages: number }>).detail;
      if (!myTeamIdRef.current || detail.teamId !== myTeamIdRef.current) return;
      if (detail.newStageOrder <= participantStageOrderRef.current) return;

      if (detail.newStageOrder > detail.totalStages) {
        // Teammate completed the last stage — stay mounted for the session's Finished switch
        setParticipantCompleted(true);
        return;
      }

      try {
        sessionStorage.setItem(`participantStage_${sessionId}`, JSON.stringify({
          participantStageOrder: detail.newStageOrder,
        }));
      } catch { }
      dispatch({ type: "STAGE_ADVANCED", participantStageOrder: detail.newStageOrder, totalStages: detail.totalStages });
    };
    window.addEventListener("TeamStageAdvanced", handler);
    return () => window.removeEventListener("TeamStageAdvanced", handler);
  }, [sessionId, dispatch]);

  const handleScan = useCallback(async (text: string) => {
    if (scanPhaseRef.current !== "idle") return;

    let payload: { stageId: string; token: string } | null = null;
    try {
      const parsed = JSON.parse(text);
      if (typeof parsed.stageId === "string" && typeof parsed.token === "string") {
        payload = parsed;
      }
    } catch { /* ignore */ }

    if (!payload) {
      const preview = text.length > 50 ? text.substring(0, 47) + "..." : text;
      setScanError(`QR no reconocido: "${preview}"`);
      setScanPhase("error");
      return;
    }

    setScanPhase("loading");

    try {
      const result = await validateQr(sessionId!, { stageId: payload.stageId, token: payload.token });

      if (!result.isValid) {
        if (result.isEliminated) {
          setGateInfo({ position: result.gatePosition, threshold: result.gateThreshold });
          setScanPhase("eliminated");
        } else {
          setScanError(result.errorMessage ?? "Código QR incorrecto para esta etapa.");
          setScanPhase("error");
        }
        return;
      }

      try {
        sessionStorage.setItem(`participantStage_${sessionId}`, JSON.stringify({
          participantStageOrder: result.currentStageOrder + 1,
        }));
      } catch { /* ignore */ }

      dispatch({
        type: "STAGE_ADVANCED",
        participantStageOrder: result.currentStageOrder + 1,
        totalStages: result.totalStages,
      });

      if (result.isAtGate && result.gateOpened) {
        setScanPhase("gate_opened");
        setTimeout(() => {
          if (result.isLastStage) { setParticipantCompleted(true); return; }
          setScanPhase("idle");
          setCameraActive(false);
        }, 2000);
        return;
      }

      if (result.isAtGate && !result.gateOpened) {
        const info = { position: result.gatePosition, threshold: result.gateThreshold };
        setGateInfo(info);
        setScanPhase("waiting_gate");
        try { sessionStorage.setItem(`gate_waiting_${sessionId}`, JSON.stringify(info)); } catch { /* ignore */ }
        return;
      }

      setScanPhase("success");
      setTimeout(() => {
        if (result.isLastStage) setParticipantCompleted(true);
        else { setScanPhase("idle"); setCameraActive(false); }
      }, 1500);

    } catch {
      setScanError("Error de conexión. Intentá de nuevo.");
      setScanPhase("error");
    }
  }, [sessionId, dispatch]);

  function retryScanner() {
    setScanError("");
    setScanPhase("idle");
    setCameraActive(false);
  }

  const showScanner = !state.isWaiting && isTreasure;
  const stageDisplay = missionStageIndex > 0 ? missionStageIndex : state.participantStageOrder;
  const stageTotal = missionStageTotal > 0 ? missionStageTotal : state.totalStages;

  // Stay mounted here (instead of navigating away) once done — GameView switches to the
  // real final-results screen automatically as soon as the session's status flips to Finished.
  if (participantCompleted) {
    return (
      <div className="mision-completada">
        <div className="mision-completada-icon">🏆</div>
        <h2>¡Completaste todas las etapas!</h2>
        <p>Esperando a que finalice la sesión para ver los resultados finales...</p>
      </div>
    );
  }

  return (
    <div className="active-game">

      {/* ── Header: mission info + score ── */}
      <div className="game-header">
        <div className="game-stage-info">
          <div className="game-mission-type-chip">
            {isTreasure ? "Búsqueda" : "Trivia"}
          </div>
          <div className="game-mission-name">{missionTitle}</div>
          {currentStageInfo?.stageName && currentStageInfo.stageName !== missionTitle && (
            <div className="game-stage-name">{currentStageInfo.stageName}</div>
          )}
          {stageTotal > 0 && (
            <div className="game-stage-counter">
              Etapa <strong>{stageDisplay}</strong> de <strong>{stageTotal}</strong>
            </div>
          )}
          {state.myTeam && (
            <div className="game-team-chip">👥 {state.myTeam.name}</div>
          )}
        </div>
        <div className="game-score-badge">
          <div className="game-score-label">Puntos</div>
          <div className="game-score-value">{state.score}</div>
        </div>
      </div>

      {/* ── Timer ── */}
      <Timer />

      {/* ── Gate waiting ── */}
      {state.isWaiting && (
        <div className="waiting-gate-banner">
          <p style={{ fontSize: 32, margin: 0 }}>⏳</p>
          <p style={{ color: "var(--color-warning)", fontWeight: 700, marginTop: 8, fontSize: 17 }}>
            Esperando a los otros jugadores...
          </p>
          <p>Posición {state.gatePosition} de {state.gateThreshold} — la barrera abre cuando lleguen todos</p>
        </div>
      )}

      {/* ── Treasure Hunt: QR scanner ── */}
      {showScanner && (
        <div className="scan-section">
          {scanPhase === "idle" && (
            <>
              {cameraActive ? (
                <>
                  <p className="scan-hint">Encontrá la ubicación y escaneá el código QR</p>
                  <QrScanner active={true} onScan={handleScan} onClose={() => setCameraActive(false)} />
                  <button onClick={() => setCameraActive(false)} className="btn-scan-close">
                    Cerrar cámara
                  </button>
                </>
              ) : (
                <button onClick={() => setCameraActive(true)} className="btn-scan">
                  📷 Escanear QR
                </button>
              )}
            </>
          )}

          {scanPhase === "loading" && (
            <div className="scan-feedback">
              <div className="spinner" />
              <p style={{ color: "var(--text-muted)", marginTop: 12 }}>Validando código...</p>
            </div>
          )}

          {scanPhase === "success" && (
            <div className="scan-feedback scan-feedback--success">
              <p className="scan-feedback-icon">✓</p>
              <p style={{ color: "var(--color-success)", fontWeight: 700, fontSize: 18 }}>¡Etapa superada!</p>
            </div>
          )}

          {scanPhase === "gate_opened" && (
            <div className="scan-feedback scan-feedback--success">
              <p className="scan-feedback-icon">🚀</p>
              <p style={{ color: "var(--color-success)", fontWeight: 700 }}>¡Barrera abierta! ¡Seguí adelante!</p>
            </div>
          )}

          {scanPhase === "waiting_gate" && (
            <div className="scan-feedback scan-feedback--warning">
              <p className="scan-feedback-icon">⏳</p>
              <p style={{ color: "var(--color-warning)", fontWeight: 700 }}>¡Etapa superada!</p>
              <p style={{ color: "var(--text-secondary)", marginTop: 8, fontSize: 14 }}>
                Fuiste el/la {gateInfo?.position}° en completar.
              </p>
              <p style={{ color: "var(--text-muted)", marginTop: 4, fontSize: 13 }}>
                Esperando los primeros {gateInfo?.threshold} jugadores para continuar...
              </p>
            </div>
          )}

          {scanPhase === "eliminated" && (
            <div className="scan-feedback scan-feedback--error">
              <p className="scan-feedback-icon">🏁</p>
              <p style={{ color: "var(--accent)", fontWeight: 700 }}>
                ¡Ya pasaron los primeros {gateInfo?.threshold}!
              </p>
              <p style={{ color: "var(--text-secondary)", marginTop: 6, fontSize: 14 }}>
                No pudiste avanzar en esta misión. Esperá el resultado final.
              </p>
            </div>
          )}

          {scanPhase === "error" && (
            <div className="scan-feedback scan-feedback--error">
              <p className="scan-feedback-icon">✗</p>
              <p style={{ color: "var(--accent)", fontWeight: 600 }}>{scanError}</p>
              <button onClick={retryScanner} className="btn btn-ghost btn-sm" style={{ marginTop: 16 }}>
                Intentar de nuevo
              </button>
            </div>
          )}
        </div>
      )}

      {/* ── Trivia: question or waiting screen ── */}
      {!isTreasure && (
        <>
          {state.currentQuestion ? (
            <QuestionCard key={state.currentQuestion.questionId} question={state.currentQuestion} />
          ) : (
            <div className="trivia-waiting">
              <div className="trivia-waiting-pulse" />
              <p className="trivia-waiting-title">Esperando pregunta</p>
              <p className="trivia-waiting-sub">El operador enviará la siguiente pregunta en breve</p>
            </div>
          )}
        </>
      )}

      {/* ── Clues (Treasure) ── */}
      {state.clues.length > 0 && (
        <div style={{ marginTop: "1rem" }}>
          {state.clues.map((clue: unknown, index: number) => (
            <ClueCard key={index} clue={clue} />
          ))}
        </div>
      )}

      {/* ── Ranking board ── */}
      <RankingBoard ranking={state.ranking} />
    </div>
  );
}

