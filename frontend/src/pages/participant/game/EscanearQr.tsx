import { useState, useCallback, useEffect, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { QrScanner } from "../../../components/QrScanner";
import { validateQr } from "../../../services/sessionsApi";

type ScanState = "scanning" | "loading" | "success" | "waiting_gate" | "gate_opened" | "eliminated" | "error";

interface QrPayload {
  stageId: string;
  token: string;
}

function parseQrPayload(text: string): QrPayload | null {
  try {
    const parsed = JSON.parse(text);
    if (typeof parsed.stageId === "string" && typeof parsed.token === "string") {
      return parsed as QrPayload;
    }
    return null;
  } catch {
    return null;
  }
}

export function EscanearQr() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();

  useEffect(() => {
    if (sessionId && !sessionStorage.getItem(`joined_${sessionId}`)) {
      navigate("/participant/sessions/join", { replace: true });
    }
  }, [sessionId, navigate]);

  const [scanState, setScanState] = useState<ScanState>("scanning");
  const [errorMessage, setErrorMessage] = useState<string>("");
  const [progress, setProgress] = useState<{ current: number; total: number } | null>(null);
  const [gateInfo, setGateInfo] = useState<{ position: number; threshold: number } | null>(null);

  // Mirror scanState into a ref so handleScan can read it without being recreated
  const scanStateRef = useRef<ScanState>("scanning");
  useEffect(() => { scanStateRef.current = scanState; }, [scanState]);

  const handleScan = useCallback(async (text: string) => {
    // Only process if the scanner is actively waiting for a scan
    if (scanStateRef.current !== "scanning") return;

    const payload = parseQrPayload(text);
    if (!payload) {
      setErrorMessage("El código QR no es válido para esta aplicación.");
      setScanState("error");
      return;
    }

    setScanState("loading");

    try {
      const result = await validateQr(sessionId!, {
        stageId: payload.stageId,
        token: payload.token,
      });

      if (!result.isValid) {
        if (result.isEliminated) {
          setGateInfo({ position: result.gatePosition, threshold: result.gateThreshold });
          setScanState("eliminated");
        } else {
          setErrorMessage(result.errorMessage ?? "Código QR incorrecto para esta etapa.");
          setScanState("error");
        }
        return;
      }

      setProgress({ current: result.currentStageOrder, total: result.totalStages });

      // Persist participant stage so ActiveGame can show it after navigating back
      try {
        sessionStorage.setItem(`participantStage_${sessionId}`, JSON.stringify({
          participantStageOrder: result.currentStageOrder + 1,
        }));
      } catch { /* ignore */ }

      if (result.isAtGate && result.gateOpened) {
        setScanState("gate_opened");
        setTimeout(() => {
          if (result.isLastStage) {
            navigate(`/juego/${sessionId}/completada`, { replace: true });
          } else {
            navigate(`/juego/${sessionId}`, { replace: true });
          }
        }, 2000);
        return;
      }

      if (result.isAtGate && !result.gateOpened) {
        const info = { position: result.gatePosition, threshold: result.gateThreshold };
        setGateInfo(info);
        setScanState("waiting_gate");
        try {
          sessionStorage.setItem(`gate_waiting_${sessionId}`, JSON.stringify(info));
        } catch { /* ignore */ }
        setTimeout(() => navigate(`/juego/${sessionId}`, { replace: true }), 2500);
        return;
      }

      // Normal advance within same mission — show success then re-enable scanner
      setScanState("success");
      setTimeout(() => {
        if (result.isLastStage) {
          navigate(`/juego/${sessionId}/completada`, { replace: true });
        } else {
          setScanState("scanning");
        }
      }, 1500);

    } catch {
      setErrorMessage("Error de conexión. Intentá de nuevo.");
      setScanState("error");
    }
  }, [sessionId, navigate]);

  function handleRetry() {
    setErrorMessage("");
    setScanState("scanning");
  }

  return (
    <div style={containerStyle}>
      <h2 style={titleStyle}>Escanear Código QR</h2>

      {progress && (
        <p style={progressStyle}>
          Etapa {progress.current} de {progress.total}
        </p>
      )}

      {scanState === "scanning" && (
        <>
          <p style={subtitleStyle}>Apuntá la cámara al código QR de la ubicación</p>
          <QrScanner active={true} onScan={handleScan} />
        </>
      )}

      {scanState === "loading" && (
        <div style={feedbackStyle}>
          <div className="spinner" />
          <p style={{ color: "#ccc", marginTop: 12 }}>Validando código...</p>
        </div>
      )}

      {scanState === "success" && (
        <div style={{ ...feedbackStyle, ...successBoxStyle }}>
          <p style={{ fontSize: 40, margin: 0 }}>✓</p>
          <p style={{ color: "#4caf50", fontWeight: 600, marginTop: 8 }}>
            {progress?.current === progress?.total ? "¡Última etapa completada!" : "¡Etapa superada!"}
          </p>
          {progress && (
            <p style={{ color: "#999", fontSize: 13, marginTop: 4 }}>
              Etapa {progress.current} de {progress.total}
            </p>
          )}
        </div>
      )}

      {scanState === "waiting_gate" && (
        <div style={{ ...feedbackStyle, border: "1px solid #f5a623", backgroundColor: "#2d2010" }}>
          <p style={{ fontSize: 36, margin: 0 }}>⏳</p>
          <p style={{ color: "#f5a623", fontWeight: 700, marginTop: 8, fontSize: 18 }}>
            ¡Etapa superada!
          </p>
          <p style={{ color: "#ccc", marginTop: 8, fontSize: 14 }}>
            Fuiste el/la {gateInfo?.position}° en completar.
          </p>
          <p style={{ color: "#aaa", marginTop: 4, fontSize: 13 }}>
            Esperando que lleguen los primeros {gateInfo?.threshold} jugadores antes de continuar...
          </p>
        </div>
      )}

      {scanState === "gate_opened" && (
        <div style={{ ...feedbackStyle, border: "1px solid #4caf50", backgroundColor: "#1a2d1a" }}>
          <p style={{ fontSize: 36, margin: 0 }}>🚀</p>
          <p style={{ color: "#4caf50", fontWeight: 700, marginTop: 8, fontSize: 18 }}>
            ¡Barrera abierta! ¡Seguí adelante!
          </p>
          <p style={{ color: "#aaa", marginTop: 4, fontSize: 13 }}>
            Completaste el top {gateInfo?.threshold}, redirigiendo...
          </p>
        </div>
      )}

      {scanState === "eliminated" && (
        <div style={{ ...feedbackStyle, ...errorBoxStyle }}>
          <p style={{ fontSize: 36, margin: 0 }}>🏁</p>
          <p style={{ color: "#e94560", fontWeight: 700, marginTop: 8, fontSize: 17 }}>
            ¡Ya pasaron los primeros {gateInfo?.threshold}!
          </p>
          <p style={{ color: "#ccc", marginTop: 6, fontSize: 14 }}>
            No pudiste avanzar en esta misión. Esperá el resultado final.
          </p>
          <button onClick={() => navigate(`/juego/${sessionId}`, { replace: true })} style={retryBtnStyle}>
            Ver el juego
          </button>
        </div>
      )}

      {scanState === "error" && (
        <div style={{ ...feedbackStyle, ...errorBoxStyle }}>
          <p style={{ fontSize: 36, margin: 0 }}>✗</p>
          <p style={{ color: "#e94560", fontWeight: 600, marginTop: 8 }}>{errorMessage}</p>
          <button onClick={handleRetry} style={retryBtnStyle}>
            Intentar de nuevo
          </button>
        </div>
      )}
    </div>
  );
}

const containerStyle: React.CSSProperties = {
  maxWidth: 480,
  margin: "0 auto",
  padding: "2rem 1rem",
  textAlign: "center",
  color: "white",
};

const titleStyle: React.CSSProperties = {
  marginBottom: "0.5rem",
};

const subtitleStyle: React.CSSProperties = {
  color: "#999",
  fontSize: 14,
  marginBottom: "1.5rem",
};

const progressStyle: React.CSSProperties = {
  color: "#aaa",
  fontSize: 13,
  marginBottom: "1rem",
  fontWeight: 500,
};

const feedbackStyle: React.CSSProperties = {
  padding: "2rem",
  borderRadius: 12,
  marginTop: "1.5rem",
};

const successBoxStyle: React.CSSProperties = {
  border: "1px solid #4caf50",
  backgroundColor: "#1a2d1a",
};

const errorBoxStyle: React.CSSProperties = {
  border: "1px solid #e94560",
  backgroundColor: "#2d1a1a",
};

const retryBtnStyle: React.CSSProperties = {
  marginTop: 16,
  padding: "10px 24px",
  backgroundColor: "#e94560",
  color: "#fff",
  border: "none",
  borderRadius: 8,
  cursor: "pointer",
  fontSize: 15,
  fontWeight: 600,
};
