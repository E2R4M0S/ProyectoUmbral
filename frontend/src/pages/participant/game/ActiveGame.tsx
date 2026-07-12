import { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useGame } from "../../../contexts/GameContext";
import { Timer } from "../../../components/game/Timer";
import { ClueCard } from "../../../components/game/ClueCard";
import { RankingBoard } from "../../../components/game/RankingBoard";
import { QuestionCard } from "../../../components/game/QuestionCard";
import { QrScanner } from "../../../components/QrScanner";
import { validateQr } from "../../../services/sessionsApi";

type ScanPhase = "idle" | "loading" | "success" | "error" | "waiting_gate" | "gate_opened" | "eliminated";

export function ActiveGame() {
  const { state, dispatch } = useGame();
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();

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

  const scanPhaseRef = useRef<ScanPhase>("idle");
  useEffect(() => { scanPhaseRef.current = scanPhase; }, [scanPhase]);

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
          setScanPhase("idle");
          setCameraActive(false);
          if (result.isLastStage) navigate(`/juego/${sessionId}/completada`, { replace: true });
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
        if (result.isLastStage) navigate(`/juego/${sessionId}/completada`, { replace: true });
        else { setScanPhase("idle"); setCameraActive(false); }
      }, 1500);

    } catch {
      setScanError("Error de conexión. Intentá de nuevo.");
      setScanPhase("error");
    }
  }, [sessionId, navigate, dispatch]);

  function retryScanner() {
    setScanError("");
    setScanPhase("idle");
    setCameraActive(false);
  }

  const showScanner = !state.isWaiting && isTreasure;
  const stageDisplay = missionStageIndex > 0 ? missionStageIndex : state.participantStageOrder;
  const stageTotal = missionStageTotal > 0 ? missionStageTotal : state.totalStages;

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
        <div style={waitingGateBannerStyle}>
          <p style={{ fontSize: 32, margin: 0 }}>⏳</p>
          <p style={{ color: "#fbbf24", fontWeight: 700, marginTop: 8, fontSize: 17 }}>
            Esperando a los otros jugadores...
          </p>
          <p style={{ color: "#aaa", fontSize: 13, marginTop: 4 }}>
            Posición {state.gatePosition} de {state.gateThreshold} — la barrera abre cuando lleguen todos
          </p>
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
                  <QrScanner active={true} onScan={handleScan} />
                  <button onClick={() => setCameraActive(false)} style={closeCamBtnStyle}>
                    Cerrar cámara
                  </button>
                </>
              ) : (
                <button onClick={() => setCameraActive(true)} style={scanBtnStyle}>
                  Escanear QR
                </button>
              )}
            </>
          )}

          {scanPhase === "loading" && (
            <div className="scan-feedback">
              <div className="spinner" />
              <p style={{ color: "#ccc", marginTop: 12 }}>Validando código...</p>
            </div>
          )}

          {scanPhase === "success" && (
            <div className="scan-feedback scan-feedback--success">
              <p style={{ fontSize: 40, margin: 0 }}>✓</p>
              <p style={{ color: "#34d399", fontWeight: 700, marginTop: 8, fontSize: 18 }}>¡Etapa superada!</p>
            </div>
          )}

          {scanPhase === "gate_opened" && (
            <div className="scan-feedback scan-feedback--success">
              <p style={{ fontSize: 40, margin: 0 }}>🚀</p>
              <p style={{ color: "#34d399", fontWeight: 700, marginTop: 8, fontSize: 17 }}>
                ¡Barrera abierta! ¡Seguí adelante!
              </p>
            </div>
          )}

          {scanPhase === "waiting_gate" && (
            <div className="scan-feedback scan-feedback--warning">
              <p style={{ fontSize: 40, margin: 0 }}>⏳</p>
              <p style={{ color: "#fbbf24", fontWeight: 700, marginTop: 8, fontSize: 17 }}>¡Etapa superada!</p>
              <p style={{ color: "#ccc", marginTop: 8, fontSize: 14 }}>
                Fuiste el/la {gateInfo?.position}° en completar.
              </p>
              <p style={{ color: "#aaa", marginTop: 4, fontSize: 13 }}>
                Esperando los primeros {gateInfo?.threshold} jugadores para continuar...
              </p>
            </div>
          )}

          {scanPhase === "eliminated" && (
            <div className="scan-feedback scan-feedback--error">
              <p style={{ fontSize: 40, margin: 0 }}>🏁</p>
              <p style={{ color: "#e94560", fontWeight: 700, marginTop: 8, fontSize: 17 }}>
                ¡Ya pasaron los primeros {gateInfo?.threshold}!
              </p>
              <p style={{ color: "#ccc", marginTop: 6, fontSize: 14 }}>
                No pudiste avanzar en esta misión. Esperá el resultado final.
              </p>
            </div>
          )}

          {scanPhase === "error" && (
            <div className="scan-feedback scan-feedback--error">
              <p style={{ fontSize: 40, margin: 0 }}>✗</p>
              <p style={{ color: "#e94560", fontWeight: 600, marginTop: 8 }}>{scanError}</p>
              <button onClick={retryScanner} style={retryBtnStyle}>Intentar de nuevo</button>
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

const waitingGateBannerStyle: React.CSSProperties = {
  margin: "1rem 0",
  padding: "1.5rem",
  backgroundColor: "#2d2010",
  border: "1px solid #fbbf24",
  borderRadius: 10,
  textAlign: "center",
};

const retryBtnStyle: React.CSSProperties = {
  marginTop: 16,
  padding: "10px 24px",
  backgroundColor: "transparent",
  color: "#e94560",
  border: "1px solid #e94560",
  borderRadius: 6,
  cursor: "pointer",
  fontSize: "0.875rem",
};

const scanBtnStyle: React.CSSProperties = {
  display: "block",
  width: "100%",
  padding: "14px 0",
  backgroundColor: "#4f46e5",
  color: "#fff",
  border: "none",
  borderRadius: 8,
  cursor: "pointer",
  fontSize: "1rem",
  fontWeight: 700,
  letterSpacing: "0.02em",
};

const closeCamBtnStyle: React.CSSProperties = {
  display: "block",
  width: "100%",
  marginTop: 12,
  padding: "12px 0",
  backgroundColor: "transparent",
  color: "#aaa",
  border: "1px solid #444",
  borderRadius: 8,
  cursor: "pointer",
  fontSize: "0.875rem",
};
