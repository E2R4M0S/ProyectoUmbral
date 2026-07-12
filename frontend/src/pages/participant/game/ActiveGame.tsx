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

  // Derive mission-relative stage info from the flat stages array
  const currentStageInfo = state.stages.find(s => s.order === state.participantStageOrder);
  const missionStages = currentStageInfo
    ? state.stages.filter(s => s.missionId === currentStageInfo.missionId).sort((a, b) => a.order - b.order)
    : [];
  const missionStageIndex = missionStages.findIndex(s => s.order === state.participantStageOrder) + 1;
  const missionStageTotal = missionStages.length;
  const missionTitle = currentStageInfo?.missionTitle ?? state.sessionName;

  // ── Inline QR scan state ───────────────────────────────────────────────────
  const [scanPhase, setScanPhase] = useState<ScanPhase>("idle");
  const [scanError, setScanError] = useState("");
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
      setScanError("El código QR no es válido para esta aplicación.");
      setScanPhase("error");
      return;
    }

    setScanPhase("loading");

    try {
      const result = await validateQr(sessionId!, {
        stageId: payload.stageId,
        token: payload.token,
      });

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

      // Save participant's personal stage progress (shared across routes via sessionStorage)
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
          if (result.isLastStage) {
            navigate(`/juego/${sessionId}/completada`, { replace: true });
          }
        }, 2000);
        return;
      }

      if (result.isAtGate && !result.gateOpened) {
        const info = { position: result.gatePosition, threshold: result.gateThreshold };
        setGateInfo(info);
        setScanPhase("waiting_gate");
        try {
          sessionStorage.setItem(`gate_waiting_${sessionId}`, JSON.stringify(info));
        } catch { /* ignore */ }
        return;
      }

      setScanPhase("success");
      setTimeout(() => {
        if (result.isLastStage) {
          navigate(`/juego/${sessionId}/completada`, { replace: true });
        } else {
          setScanPhase("idle");
        }
      }, 1500);

    } catch {
      setScanError("Error de conexión. Intentá de nuevo.");
      setScanPhase("error");
    }
  }, [sessionId, navigate, dispatch]);

  function retryScanner() {
    setScanError("");
    setScanPhase("idle");
  }

  const showScanner = !state.isWaiting && isTreasure;

  return (
    <div className="active-game">
      {/* Mission + stage badge */}
      {state.totalStages > 0 && (
        <div style={stageBadgeStyle}>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ color: "#aaa", fontSize: 11, textTransform: "uppercase", letterSpacing: 1 }}>
              {missionTitle}
            </div>
            <div style={{ display: "flex", alignItems: "baseline", gap: 4, marginTop: 2 }}>
              <span style={{ color: "white", fontWeight: 700, fontSize: 20, lineHeight: 1 }}>
                {missionStageIndex > 0 ? missionStageIndex : state.participantStageOrder}
              </span>
              <span style={{ color: "#666", fontSize: 12 }}>
                / {missionStageTotal > 0 ? missionStageTotal : state.totalStages}
              </span>
            </div>
          </div>
        </div>
      )}

      <Timer />
      <RankingBoard ranking={state.ranking} />

      {/* Gate waiting */}
      {state.isWaiting && (
        <div style={waitingGateBannerStyle}>
          <p style={{ fontSize: 32, margin: 0 }}>⏳</p>
          <p style={{ color: "#f5a623", fontWeight: 700, marginTop: 8, fontSize: 17 }}>
            Esperando a los otros jugadores...
          </p>
          <p style={{ color: "#aaa", fontSize: 13, marginTop: 4 }}>
            Posición {state.gatePosition} de {state.gateThreshold} — la barrera abre cuando lleguen todos
          </p>
        </div>
      )}

      {/* Inline QR scanner for Treasure Hunt */}
      {showScanner && (
        <div style={scanSectionStyle}>
          {scanPhase === "idle" && (
            <>
              <p style={scanHintStyle}>Encontrá la ubicación y escaneá el código QR</p>
              <QrScanner active={true} onScan={handleScan} />
            </>
          )}

          {scanPhase === "loading" && (
            <div style={feedbackStyle}>
              <div className="spinner" />
              <p style={{ color: "#ccc", marginTop: 12 }}>Validando código...</p>
            </div>
          )}

          {scanPhase === "success" && (
            <div style={{ ...feedbackStyle, border: "1px solid #4caf50", backgroundColor: "#1a2d1a" }}>
              <p style={{ fontSize: 36, margin: 0 }}>✓</p>
              <p style={{ color: "#4caf50", fontWeight: 600, marginTop: 8 }}>¡Etapa superada!</p>
            </div>
          )}

          {scanPhase === "gate_opened" && (
            <div style={{ ...feedbackStyle, border: "1px solid #4caf50", backgroundColor: "#1a2d1a" }}>
              <p style={{ fontSize: 36, margin: 0 }}>🚀</p>
              <p style={{ color: "#4caf50", fontWeight: 700, marginTop: 8, fontSize: 17 }}>
                ¡Barrera abierta! ¡Seguí adelante!
              </p>
            </div>
          )}

          {scanPhase === "waiting_gate" && (
            <div style={{ ...feedbackStyle, border: "1px solid #f5a623", backgroundColor: "#2d2010" }}>
              <p style={{ fontSize: 36, margin: 0 }}>⏳</p>
              <p style={{ color: "#f5a623", fontWeight: 700, marginTop: 8, fontSize: 17 }}>¡Etapa superada!</p>
              <p style={{ color: "#ccc", marginTop: 8, fontSize: 14 }}>
                Fuiste el/la {gateInfo?.position}° en completar.
              </p>
              <p style={{ color: "#aaa", marginTop: 4, fontSize: 13 }}>
                Esperando los primeros {gateInfo?.threshold} jugadores para continuar...
              </p>
            </div>
          )}

          {scanPhase === "eliminated" && (
            <div style={{ ...feedbackStyle, border: "1px solid #e94560", backgroundColor: "#2d1a1a" }}>
              <p style={{ fontSize: 36, margin: 0 }}>🏁</p>
              <p style={{ color: "#e94560", fontWeight: 700, marginTop: 8, fontSize: 17 }}>
                ¡Ya pasaron los primeros {gateInfo?.threshold}!
              </p>
              <p style={{ color: "#ccc", marginTop: 6, fontSize: 14 }}>
                No pudiste avanzar en esta misión. Esperá el resultado final.
              </p>
            </div>
          )}

          {scanPhase === "error" && (
            <div style={{ ...feedbackStyle, border: "1px solid #e94560", backgroundColor: "#2d1a1a" }}>
              <p style={{ fontSize: 36, margin: 0 }}>✗</p>
              <p style={{ color: "#e94560", fontWeight: 600, marginTop: 8 }}>{scanError}</p>
              <button onClick={retryScanner} style={retryBtnStyle}>Intentar de nuevo</button>
            </div>
          )}
        </div>
      )}

      {state.currentQuestion && (
        <QuestionCard key={state.currentQuestion.questionId} question={state.currentQuestion} />
      )}

      {!state.currentQuestion && state.clues.length === 0 && !isTreasure ? (
        <div className="waiting-msg">
          {state.sessionStatus === "Active" ? "Esperando contenido..." : "Aún no hay pistas disponibles. ¡Prestá atención!"}
        </div>
      ) : !state.currentQuestion && state.clues.length > 0 ? (
        <div style={{ marginTop: "1rem" }}>
          {state.clues.map((clue: unknown, index: number) => (
            <ClueCard key={index} clue={clue} />
          ))}
        </div>
      ) : null}
    </div>
  );
}

const stageBadgeStyle: React.CSSProperties = {
  display: "flex",
  alignItems: "baseline",
  gap: 6,
  padding: "0.5rem 1rem",
  backgroundColor: "#0d1b35",
  border: "1px solid #0f3460",
  borderRadius: 8,
  marginBottom: "1rem",
};

const waitingGateBannerStyle: React.CSSProperties = {
  margin: "1rem 0",
  padding: "1.5rem",
  backgroundColor: "#2d2010",
  border: "1px solid #f5a623",
  borderRadius: 10,
  textAlign: "center",
};

const scanSectionStyle: React.CSSProperties = {
  margin: "1rem 0",
  padding: "1.25rem",
  backgroundColor: "#0d1b35",
  border: "1px solid #0f3460",
  borderRadius: 10,
  textAlign: "center",
};

const scanHintStyle: React.CSSProperties = {
  color: "#aaa",
  fontSize: "0.875rem",
  marginBottom: "1rem",
};

const scanBtnStyle: React.CSSProperties = {
  display: "inline-block",
  padding: "14px 32px",
  backgroundColor: "#e94560",
  color: "white",
  borderRadius: 8,
  cursor: "pointer",
  fontSize: "1rem",
  fontWeight: 700,
  letterSpacing: 0.5,
};

const feedbackStyle: React.CSSProperties = {
  padding: "1.5rem",
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
